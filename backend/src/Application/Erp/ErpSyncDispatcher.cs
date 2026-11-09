using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Domain.Entities.Auctions;
using ProcureLive.Domain.Entities.Erp;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Erp;

/// <summary>
/// ERP yazma kuyruğunu işler (arka plan servisi her 10 sn'de çağırır). Premium anahtarı kapalıysa hiçbir şey yapmaz.
/// 1) Eşitleme: yayınlanmış (Planlandı/Canlı/Duraklatıldı) ihalelerde ERP teklifi olmayan davetliler için teklif açma işi,
///    ERP'ye son yansıtılandan yeni teklifi olan tedarikçiler için revizyon işi, ayarlıysa ERP kodu olmayan aktif tedarikçi/ürün için
///    gönderim işi oluşturur. Böylece davet, katılım talebi, davet linki, ön teklif gibi her yol tek yerden yakalanır.
/// 2) Gönderim: XML gönderim anında güncel veriden üretilir. Simülasyonda gönderilmez, kayda "Simulated" yazılır; canlıda
///    UyumSaveWebService çağrılır, hata olursa 1 ve 5 dk sonra yeniden denenir, 3. hatada "Failed".
/// SaveOffer/SaveOrderM kimlik döndürmediği için ERP kimlikleri belge numarasıyla GetDataTableWithSQL üzerinden geri okunur.
/// </summary>
public sealed class ErpSyncDispatcher
{
    public const int MaxAttempts = 3;
    private const int BatchSize = 20;

    private static readonly AuctionStatus[] PublishedStatuses = [AuctionStatus.Scheduled, AuctionStatus.Live, AuctionStatus.Paused];

    private readonly IApplicationDbContext _context;
    private readonly IFeatureFlags _features;
    private readonly IErpSettingsProvider _provider;
    private readonly IErpClient _client;
    private readonly IDateTime _dateTime;
    private readonly ILogger<ErpSyncDispatcher> _logger;

    public ErpSyncDispatcher(
        IApplicationDbContext context, IFeatureFlags features, IErpSettingsProvider provider, IErpClient client,
        IDateTime dateTime, ILogger<ErpSyncDispatcher> logger)
    {
        _context = context;
        _features = features;
        _provider = provider;
        _client = client;
        _dateTime = dateTime;
        _logger = logger;
    }

    public async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        if (!_features.ErpIntegration) return;
        ErpConnection c = await _provider.GetAsync(cancellationToken);

        if (c.Mode == ErpMode.Off)
        {
            // Bekleyen işler biriktirilmez: entegrasyon kapatıldıysa atlanır (Admin istediğini yeniden çalıştırabilir).
            List<ErpSyncJob> pending = await _context.ErpSyncJobs.Where(j => j.Status == ErpSyncStatus.Pending).Take(200).ToListAsync(cancellationToken);
            foreach (ErpSyncJob job in pending) Finish(job, ErpSyncStatus.Skipped, "ERP entegrasyonu kapalı.");
            if (pending.Count > 0) await _context.SaveChangesAsync(cancellationToken);
            return;
        }

        await ReconcileAsync(c, cancellationToken);
        await ProcessDueAsync(c, cancellationToken);
    }

    // ---------------------------------------------------------------- 1) Eşitleme

    private async Task ReconcileAsync(ErpConnection c, CancellationToken ct)
    {
        DateTime now = _dateTime.UtcNow;

        // Yayınlanmış ihalede ERP teklifi olmayan davetliler (reddedenler hariç).
        var missing = await _context.AuctionInvitations.AsNoTracking()
            .Where(i => PublishedStatuses.Contains(i.Auction.Status) && i.Status != InvitationStatus.Declined)
            .Where(i => !_context.ErpOfferLinks.Any(l => l.AuctionId == i.AuctionId && l.CompanyId == i.CompanyId))
            .Select(i => new { i.AuctionId, i.CompanyId, i.Auction.ReferenceCode, i.Auction.Title, CompanyCode = i.Company.Code, CompanyName = i.Company.Name })
            .Take(BatchSize).ToListAsync(ct);
        foreach (var m in missing)
        {
            string docNo = $"{c.DocNoPrefix}{m.ReferenceCode ?? m.AuctionId.ToString("N")[..8]}-{m.CompanyCode}".ToUpperInvariant();
            _context.ErpOfferLinks.Add(new ErpOfferLink { AuctionId = m.AuctionId, CompanyId = m.CompanyId, DocNo = docNo[..Math.Min(docNo.Length, 40)], CreatedAt = now });
            AddJob(ErpSyncKind.OfferCreate, $"Teklif aç · {m.ReferenceCode} · {m.CompanyName}", j => { j.AuctionId = m.AuctionId; j.CompanyId = m.CompanyId; });
        }

        // ERP'ye yansıtılandan yeni teklifi olan tedarikçiler → revizyon (bekleyen revizyon varsa yenisi açılmaz).
        var revisions = await _context.ErpOfferLinks.AsNoTracking()
            .Select(l => new
            {
                l.AuctionId, l.CompanyId, l.LastRevisedBidAt,
                LastBid = _context.Bids.Where(b => b.AuctionItem.AuctionId == l.AuctionId && b.CompanyId == l.CompanyId).Max(b => (DateTime?)b.BidTime)
            })
            .Where(x => x.LastBid != null && (x.LastRevisedBidAt == null || x.LastBid > x.LastRevisedBidAt))
            .Where(x => !_context.ErpSyncJobs.Any(j => j.AuctionId == x.AuctionId && j.CompanyId == x.CompanyId
                && (j.Kind == ErpSyncKind.OfferRevise || j.Kind == ErpSyncKind.OfferCreate) && j.Status == ErpSyncStatus.Pending))
            .Take(BatchSize).ToListAsync(ct);
        foreach (var r in revisions)
        {
            string label = await _context.Auctions.Where(a => a.Id == r.AuctionId).Select(a => a.ReferenceCode).FirstOrDefaultAsync(ct) ?? "";
            string company = await _context.Companies.IgnoreQueryFilters().Where(x => x.Id == r.CompanyId).Select(x => x.Name).FirstOrDefaultAsync(ct) ?? "";
            AddJob(ErpSyncKind.OfferRevise, $"Teklif revize · {label} · {company}", j => { j.AuctionId = r.AuctionId; j.CompanyId = r.CompanyId; });
        }

        if (c.ExportNewSuppliers)
        {
            var suppliers = await _context.Companies.AsNoTracking()
                .Where(x => x.CompanyType == CompanyType.Supplier && x.Status == CompanyStatus.Active && x.ErpEntityCode == null)
                .Where(x => !_context.ErpSyncJobs.Any(j => j.Kind == ErpSyncKind.EntityExport && j.CompanyId == x.Id))
                .Select(x => new { x.Id, x.Code, x.Name }).Take(BatchSize).ToListAsync(ct);
            foreach (var s in suppliers)
                AddJob(ErpSyncKind.EntityExport, $"Cari · {s.Code} {s.Name}", j => j.CompanyId = s.Id);
        }

        if (c.ExportNewProducts)
        {
            var products = await _context.Products.AsNoTracking()
                .Where(x => x.IsActive && x.ErpItemCode == null)
                .Where(x => !_context.ErpSyncJobs.Any(j => j.Kind == ErpSyncKind.ItemExport && j.ProductId == x.Id))
                .Select(x => new { x.Id, x.Sku, x.Name }).Take(BatchSize).ToListAsync(ct);
            foreach (var p in products)
                AddJob(ErpSyncKind.ItemExport, $"Ürün · {p.Sku} {p.Name}", j => j.ProductId = p.Id);
        }

        await _context.SaveChangesAsync(ct);
    }

    private void AddJob(ErpSyncKind kind, string summary, Action<ErpSyncJob> target)
    {
        var job = new ErpSyncJob { Kind = kind, Summary = summary, CreatedAt = _dateTime.UtcNow };
        target(job);
        _context.ErpSyncJobs.Add(job);
    }

    // ---------------------------------------------------------------- 2) Gönderim

    private async Task ProcessDueAsync(ErpConnection c, CancellationToken ct)
    {
        DateTime now = _dateTime.UtcNow;
        List<ErpSyncJob> due = await _context.ErpSyncJobs
            .Where(j => j.Status == ErpSyncStatus.Pending && (j.NextAttemptAt == null || j.NextAttemptAt <= now))
            // Cari ve ürün önce: aynı turda açılacak teklif/sipariş onların ERP koduna ihtiyaç duyar.
            .OrderBy(j => j.Kind == ErpSyncKind.EntityExport || j.Kind == ErpSyncKind.ItemExport ? 0 : 1).ThenBy(j => j.CreatedAt)
            .Take(BatchSize).ToListAsync(ct);

        foreach (ErpSyncJob job in due)
        {
            job.AttemptCount++;
            try
            {
                await (job.Kind switch
                {
                    ErpSyncKind.EntityExport => EntityExportAsync(c, job, ct),
                    ErpSyncKind.ItemExport => ItemExportAsync(c, job, ct),
                    ErpSyncKind.OfferCreate => OfferCreateAsync(c, job, ct),
                    ErpSyncKind.OfferRevise => OfferReviseAsync(c, job, ct),
                    ErpSyncKind.OrderCreate => OrderCreateAsync(c, job, ct),
                    _ => throw new InvalidOperationException("Bilinmeyen iş türü.")
                });
            }
            catch (ErpSkipException skip)
            {
                Finish(job, ErpSyncStatus.Skipped, skip.Message);
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                job.Message = ex.Message.Length > 900 ? ex.Message[..900] : ex.Message;
                if (job.AttemptCount >= MaxAttempts)
                {
                    job.Status = ErpSyncStatus.Failed;
                    job.NextAttemptAt = null;
                    job.CompletedAt = _dateTime.UtcNow;
                }
                else
                {
                    job.NextAttemptAt = _dateTime.UtcNow.AddMinutes(job.AttemptCount == 1 ? 1 : 5);
                }

                _logger.LogWarning(ex, "ERP işi başarısız ({Attempt}/{Max}): {Summary}", job.AttemptCount, MaxAttempts, job.Summary);
            }

            // Her işten sonra kaydet: ERP'ye gitmiş bir işin sonucu süreç kapanırsa kaybolmasın.
            await _context.SaveChangesAsync(ct);
        }
    }

    private async Task EntityExportAsync(ErpConnection c, ErpSyncJob job, CancellationToken ct)
    {
        var company = await _context.Companies.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == job.CompanyId, ct)
            ?? throw new ErpSkipException("Cari bulunamadı.");
        string code = company.ErpEntityCode ?? company.Code ?? throw new ErpSkipException("Carinin kodu yok.");
        string xml = ErpXml.Entity(c.CoCode, code, company.Name, company.TaxNumber, company.TaxOffice, company.Phone, company.Email, company.Website);

        if (!await SendAsync(c, job, ErpSaveOperation.SaveEntity, xml, ct)) return;
        company.ErpEntityCode = code;
        company.ErpEntityId = await LookupIdAsync(c, $"SELECT ENTITY_ID AS ID FROM FIND_ENTITY WHERE ENTITY_CODE = {ErpXml.Sql(code)}", ct) ?? company.ErpEntityId;
    }

    private async Task ItemExportAsync(ErpConnection c, ErpSyncJob job, CancellationToken ct)
    {
        var product = await _context.Products.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Id == job.ProductId, ct)
            ?? throw new ErpSkipException("Ürün bulunamadı.");
        string code = product.ErpItemCode ?? product.Sku;
        string xml = ErpXml.Item(c.CoCode, c.BranchCode, code, product.Name, ErpUnits.Code(product.UnitOfMeasure), product.Brand);

        if (!await SendAsync(c, job, ErpSaveOperation.SaveItem, xml, ct)) return;
        product.ErpItemCode = code;
        product.ErpItemId = await LookupIdAsync(c, $"SELECT ITEM_ID AS ID FROM INVD_ITEM WHERE ITEM_CODE = {ErpXml.Sql(code)}", ct) ?? product.ErpItemId;
    }

    private async Task OfferCreateAsync(ErpConnection c, ErpSyncJob job, CancellationToken ct)
    {
        ErpOfferLink link = await _context.ErpOfferLinks.FirstOrDefaultAsync(l => l.AuctionId == job.AuctionId && l.CompanyId == job.CompanyId, ct)
            ?? throw new ErpSkipException("Teklif bağlantısı bulunamadı.");
        Auction auction = await _context.Auctions.Include(a => a.Currency).Include(a => a.Items).ThenInclude(i => i.Product)
            .FirstAsync(a => a.Id == link.AuctionId, ct);
        string entityCode = await EntityCodeAsync(c, link.CompanyId, ct);

        List<AuctionItem> items = auction.Items.OrderBy(i => i.SortOrder).ToList();
        (List<ErpXml.Line> lines, List<Guid> sentItemIds, List<string> skipped) = BuildLines(c, items, auction.Currency.Code);
        if (lines.Count == 0) throw new ErpSkipException("ERP stok kodu eşleşen kalem yok: " + string.Join(", ", skipped));

        string xml = ErpXml.Document(false, c.CoCode, c.BranchCode, c.OfferDocTraCode, link.DocNo, _dateTime.UtcNow, entityCode,
            auction.Currency.Code, $"ProcureLive ihale {auction.ReferenceCode}", auction.Title, lines);
        string? note = skipped.Count > 0 ? "ERP stok kodu olmadığı için gönderilmeyen kalemler: " + string.Join(", ", skipped) : null;

        if (!await SendAsync(c, job, ErpSaveOperation.SaveOffer, xml, ct, note)) return;

        // Kimlik ve satır eşlemesi: belge numarasıyla geri oku; satır numarası bizim gönderdiğimiz sıra.
        var rows = await _client.QueryAsync(c,
            "SELECT M.OFFER_M_ID, D.OFFER_D_ID, D.LINE_NO FROM PSMT_OFFER_M M INNER JOIN PSMT_OFFER_D D ON D.OFFER_M_ID = M.OFFER_M_ID "
            + $"WHERE M.DOC_NO = {ErpXml.Sql(link.DocNo)} ORDER BY D.LINE_NO", ct);
        if (rows.Count == 0) throw new InvalidOperationException($"Teklif kaydedildi ama ERP'de {link.DocNo} belge numarasıyla bulunamadı.");

        link.ErpOfferMId = ErpRows.Int(rows[0], "OFFER_M_ID");
        var map = new Dictionary<Guid, int>();
        for (int n = 0; n < sentItemIds.Count && n < rows.Count; n++) map[sentItemIds[n]] = ErpRows.Int(rows[n], "OFFER_D_ID");
        link.LineMapJson = JsonSerializer.Serialize(map);
        job.Message = $"ERP teklif no {link.ErpOfferMId}" + (note is null ? "" : $" · {note}");
    }

    private async Task OfferReviseAsync(ErpConnection c, ErpSyncJob job, CancellationToken ct)
    {
        ErpOfferLink link = await _context.ErpOfferLinks.FirstOrDefaultAsync(l => l.AuctionId == job.AuctionId && l.CompanyId == job.CompanyId, ct)
            ?? throw new ErpSkipException("Teklif bağlantısı bulunamadı.");
        Auction auction = await _context.Auctions.Include(a => a.Currency).Include(a => a.Items).FirstAsync(a => a.Id == link.AuctionId, ct);

        // Tedarikçinin her kalemdeki en son teklifi (birim fiyat). Geri çekilen/reddedilen teklifler sayılmaz.
        var bids = await _context.Bids.AsNoTracking()
            .Where(b => b.AuctionItem.AuctionId == link.AuctionId && b.CompanyId == link.CompanyId
                        && b.Status != BidStatus.Withdrawn && b.Status != BidStatus.Rejected)
            .Select(b => new { b.AuctionItemId, Amount = b.Amount.Amount, b.BidTime }).ToListAsync(ct);
        if (bids.Count == 0) throw new ErpSkipException("Tedarikçinin geçerli teklifi yok.");
        DateTime lastBid = bids.Max(b => b.BidTime);
        var latest = bids.GroupBy(b => b.AuctionItemId).ToDictionary(g => g.Key, g => g.OrderByDescending(b => b.BidTime).First());

        if (c.Mode == ErpMode.Live && link.ErpOfferMId is null)
            throw new InvalidOperationException("ERP teklifi henüz açılmadı (teklif açma işi bekleniyor).");

        Dictionary<Guid, int> map = link.LineMapJson is null ? new() : JsonSerializer.Deserialize<Dictionary<Guid, int>>(link.LineMapJson) ?? new();
        decimal rate = await RateAsync(auction.Currency.Code, auction.CurrencyId, ct);
        var lines = auction.Items.OrderBy(i => i.SortOrder)
            .Where(i => latest.ContainsKey(i.Id) && (c.Mode == ErpMode.Simulation || map.ContainsKey(i.Id)))
            .Select(i => new ErpXml.RevisionLine(map.GetValueOrDefault(i.Id), latest[i.Id].Amount, auction.Currency.Code, rate,
                i.RequestedDeliveryDate, $"ProcureLive teklifi {latest[i.Id].BidTime:dd.MM.yyyy HH:mm} UTC"))
            .ToList();
        if (lines.Count == 0) throw new ErpSkipException("ERP teklif satırıyla eşleşen teklif yok.");

        string xml = ErpXml.OfferRevision(link.ErpOfferMId ?? 0, lines);
        string? note = c.Mode == ErpMode.Simulation && link.ErpOfferMId is null ? "Simülasyonda ERP kimlikleri bilinmediği için 0 yazıldı." : null;
        await SendAsync(c, job, ErpSaveOperation.SaveUyumObjectFromXML, xml, ct, note);
        link.LastRevisedBidAt = lastBid;
    }

    private async Task OrderCreateAsync(ErpConnection c, ErpSyncJob job, CancellationToken ct)
    {
        var po = await _context.PurchaseOrders.Include(p => p.Lines).Include(p => p.Currency)
            .FirstOrDefaultAsync(p => p.Id == job.PurchaseOrderId, ct) ?? throw new ErpSkipException("Sipariş bulunamadı.");
        if (po.ErpOrderDocNo is not null) throw new ErpSkipException($"Sipariş ERP'de zaten var ({po.ErpOrderDocNo}).");
        string entityCode = await EntityCodeAsync(c, po.SupplierCompanyId, ct);

        // Sipariş satırının ürünü kaynak süreç kalemi üzerinden bulunur (ihale kalemi veya RFQ satırı).
        var sourceIds = po.Lines.Where(l => l.SourceItemId != null).Select(l => l.SourceItemId!.Value).ToList();
        Dictionary<Guid, Guid?> productBySource = po.SourceKind == ProcessKind.Rfq
            ? await _context.RfqLineItems.Where(x => sourceIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.ProductId, ct)
            : await _context.AuctionItems.Where(x => sourceIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.ProductId, ct);
        List<Guid> productIds = productBySource.Values.OfType<Guid>().ToList();
        var products = await _context.Products.IgnoreQueryFilters().Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => new { p.ErpItemCode, p.Sku }, ct);

        var lines = new List<ErpXml.Line>();
        var skipped = new List<string>();
        int n = 0;
        foreach (var l in po.Lines.OrderBy(x => x.SortOrder))
        {
            Guid? productId = l.SourceItemId is Guid s && productBySource.TryGetValue(s, out Guid? pid) ? pid : null;
            string? code = productId is Guid p && products.TryGetValue(p, out var pr) ? pr.ErpItemCode ?? (c.Mode == ErpMode.Simulation ? pr.Sku : null) : null;
            if (code is null) { skipped.Add(l.Description); continue; }
            lines.Add(new ErpXml.Line(++n, code, ErpUnits.Code(l.UnitOfMeasure), l.Quantity, l.UnitPrice, po.Currency.Code, po.DeliveryDate, l.Description));
        }
        if (lines.Count == 0) throw new ErpSkipException("ERP stok kodu eşleşen satır yok: " + string.Join(", ", skipped));

        string docNo = $"{c.DocNoPrefix}{po.OrderNumber}".ToUpperInvariant();
        string xml = ErpXml.Document(true, c.CoCode, c.BranchCode, c.OrderDocTraCode, docNo, _dateTime.UtcNow, entityCode, po.Currency.Code,
            $"ProcureLive sipariş {po.OrderNumber}", po.SourceTitle, lines);
        string? note = skipped.Count > 0 ? "ERP stok kodu olmadığı için gönderilmeyen satırlar: " + string.Join(", ", skipped) : null;

        if (!await SendAsync(c, job, ErpSaveOperation.SaveOrderM, xml, ct, note)) return;
        po.ErpOrderDocNo = docNo;
        job.Message = $"ERP sipariş belge no {docNo}" + (note is null ? "" : $" · {note}");
    }

    // ---------------------------------------------------------------- Ortak

    /// <summary>Simülasyonda isteği kaydedip false döner; canlıda gönderir, ERP başarısız derse istisna fırlatır (yeniden denenir).</summary>
    private async Task<bool> SendAsync(ErpConnection c, ErpSyncJob job, ErpSaveOperation op, string xml, CancellationToken ct, string? note = null)
    {
        job.RequestXml = _client.DescribeEnvelope(c, op, xml);
        if (c.Mode == ErpMode.Simulation)
        {
            Finish(job, ErpSyncStatus.Simulated, "Simülasyon: ERP'ye gönderilmedi." + (note is null ? "" : $" {note}"));
            return false;
        }

        ErpCallResult result = await _client.SaveAsync(c, op, xml, ct);
        job.ResponseText = result.RawResponse.Length > 20000 ? result.RawResponse[..20000] : result.RawResponse;
        if (!result.Success) throw new InvalidOperationException("ERP reddetti: " + (result.Message ?? "mesaj yok"));
        Finish(job, ErpSyncStatus.Done, result.Message ?? note);
        return true;
    }

    private void Finish(ErpSyncJob job, ErpSyncStatus status, string? message)
    {
        job.Status = status;
        job.Message = message;
        job.NextAttemptAt = null;
        job.CompletedAt = _dateTime.UtcNow;
    }

    /// <summary>Fiyatsız teklif satırları; gönderilen kalemlerin sırası ERP satır numarasıyla eşlenir (SentItemIds[n] ↔ LINE_NO n+1).</summary>
    private static (List<ErpXml.Line> Lines, List<Guid> SentItemIds, List<string> Skipped) BuildLines(ErpConnection c, List<AuctionItem> items, string curCode)
    {
        var lines = new List<ErpXml.Line>();
        var sent = new List<Guid>();
        var skipped = new List<string>();
        foreach (AuctionItem i in items)
        {
            string? code = i.Product?.ErpItemCode ?? (c.Mode == ErpMode.Simulation ? i.Product?.Sku : null);
            if (code is null) { skipped.Add(i.Description); continue; }
            lines.Add(new ErpXml.Line(lines.Count + 1, code, ErpUnits.Code(i.UnitOfMeasure), i.Quantity, 0m, curCode, i.RequestedDeliveryDate, i.Description));
            sent.Add(i.Id);
        }

        return (lines, sent, skipped);
    }

    private async Task<string> EntityCodeAsync(ErpConnection c, Guid companyId, CancellationToken ct)
    {
        var company = await _context.Companies.IgnoreQueryFilters().Where(x => x.Id == companyId)
            .Select(x => new { x.ErpEntityCode, x.Code, x.Name }).FirstAsync(ct);
        if (company.ErpEntityCode is not null) return company.ErpEntityCode;
        if (c.Mode == ErpMode.Simulation) return company.Code ?? "";
        // Canlıda cari önce ERP'ye gönderilmeli (otomatik gönderim açıksa eşitleme bunu yapar); o zamana kadar yeniden denenir.
        throw new InvalidOperationException($"\"{company.Name}\" ERP'de eşleşmemiş. Cari kartından ERP'ye gönderin veya ERP'den aktarıp eşleyin.");
    }

    private async Task<decimal> RateAsync(string curCode, Guid currencyId, CancellationToken ct)
    {
        if (curCode is "TRY" or "TL") return 1m;
        decimal? rate = await _context.ExchangeRates.Where(r => r.CurrencyId == currencyId)
            .OrderByDescending(r => r.RateDate).Select(r => (decimal?)r.RateToBase).FirstOrDefaultAsync(ct);
        return rate ?? 1m;
    }

    private async Task<int?> LookupIdAsync(ErpConnection c, string sql, CancellationToken ct)
    {
        try
        {
            var rows = await _client.QueryAsync(c, sql, ct);
            return rows.Count > 0 && ErpRows.Int(rows[0], "ID") is int id and > 0 ? id : null;
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "ERP kimliği okunamadı: {Sql}", sql);
            return null;
        }
    }

    /// <summary>İş yapılamaz ve yeniden denemek anlamsız (eşleme yok, kayıt silinmiş): "Skipped".</summary>
    private sealed class ErpSkipException(string message) : Exception(message);
}
