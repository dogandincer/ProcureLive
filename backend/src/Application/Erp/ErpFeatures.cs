using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Application.Common.Security;
using ProcureLive.Application.Companies;
using ProcureLive.Contracts.Common;
using ProcureLive.Contracts.Erp;
using ProcureLive.Domain.Entities.Catalog;
using ProcureLive.Domain.Entities.Companies;
using ProcureLive.Domain.Entities.Erp;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Erp;

// Erişim: ayar/aktarım/kayıt uçları controller'da Admin politikasıyla; kart durumu iç kullanıcılara, sipariş aktarımı
// orders.manage yetkisiyle. Hepsi önce premium anahtarını (Features:ErpIntegration) denetler.

/// <summary>Premium anahtarı ve çalışma modu denetimi.</summary>
public static class ErpGuard
{
    public static void EnsureLicensed(IFeatureFlags features)
    {
        if (!features.ErpIntegration)
            throw new ForbiddenAccessException("ERP entegrasyonu premium bir özelliktir ve bu kurulumda açık değil.", ErrorCodes.PremiumRequired);
    }

    public static async Task<ErpConnection> RequireEnabledAsync(IFeatureFlags features, IErpSettingsProvider provider, CancellationToken ct)
    {
        EnsureLicensed(features);
        ErpConnection connection = await provider.GetAsync(ct);
        if (connection.Mode == ErpMode.Off)
            throw new ConflictException("ERP entegrasyonu kapalı. ERP Entegrasyonu ekranından Simülasyon veya Canlı modu seçin.");
        if (!connection.CanRead)
            throw new ConflictException("ERP bağlantı bilgileri eksik (okuma servisi adresi ve kullanıcı adı).");
        return connection;
    }
}

/// <summary>ProcureLive birimi ↔ ERP birim kodu. ERP'deki kodlar kuruluma göre değişebilir; eşleşmeyen ERP birimi "Adet" sayılır.</summary>
public static class ErpUnits
{
    private static readonly Dictionary<UnitOfMeasure, string> ToErp = new()
    {
        [UnitOfMeasure.Piece] = "ADET", [UnitOfMeasure.Kilogram] = "KG", [UnitOfMeasure.Gram] = "GR", [UnitOfMeasure.Liter] = "LT",
        [UnitOfMeasure.Meter] = "MT", [UnitOfMeasure.SquareMeter] = "M2", [UnitOfMeasure.CubicMeter] = "M3", [UnitOfMeasure.Box] = "KUTU",
        [UnitOfMeasure.Pallet] = "PALET", [UnitOfMeasure.Hour] = "SAAT", [UnitOfMeasure.Service] = "ADET"
    };

    public static string Code(UnitOfMeasure unit) => ToErp[unit];

    public static UnitOfMeasure Parse(string? erpCode)
    {
        string c = (erpCode ?? "").Trim().ToUpperInvariant();
        foreach ((UnitOfMeasure unit, string code) in ToErp)
            if (code == c) return unit;
        return c switch { "AD" or "PCS" or "EA" => UnitOfMeasure.Piece, "M" or "METRE" => UnitOfMeasure.Meter, "L" or "LITRE" => UnitOfMeasure.Liter, _ => UnitOfMeasure.Piece };
    }
}

/// <summary>ERP okuma sorguları ve satır alanlarının esnek okunması (kolon adları kuruluma göre farklı olabilir).</summary>
internal static class ErpRows
{
    public static string? Pick(IReadOnlyDictionary<string, string?> row, params string[] keys)
    {
        foreach (string key in keys)
            if (row.TryGetValue(key, out string? v) && !string.IsNullOrWhiteSpace(v)) return v.Trim();
        return null;
    }

    public static int Int(IReadOnlyDictionary<string, string?> row, string key) => int.TryParse(Pick(row, key), out int v) ? v : 0;

    public static string EntitySql(ErpConnection c, string? search, IReadOnlyCollection<string>? codes)
    {
        var where = new List<string>();
        if (!string.IsNullOrWhiteSpace(c.EntityCodePrefix)) where.Add($"ENTITY_CODE LIKE {ErpXml.Sql(c.EntityCodePrefix.Trim() + "%")}");
        if (!string.IsNullOrWhiteSpace(search))
        {
            string like = ErpXml.Sql("%" + search.Trim().ToUpperInvariant() + "%");
            where.Add($"(UPPER(ENTITY_CODE) LIKE {like} OR UPPER(ENTITY_NAME) LIKE {like})");
        }
        if (codes is { Count: > 0 }) where.Add($"ENTITY_CODE IN ({string.Join(",", codes.Select(ErpXml.Sql))})");
        return $"SELECT * FROM FIND_ENTITY{(where.Count > 0 ? " WHERE " + string.Join(" AND ", where) : "")} ORDER BY ENTITY_CODE LIMIT {(codes is { Count: > 0 } ? codes.Count : 50)}";
    }

    public static string ItemSql(string? search, IReadOnlyCollection<string>? codes)
    {
        var where = new List<string>();
        if (!string.IsNullOrWhiteSpace(search))
        {
            string like = ErpXml.Sql("%" + search.Trim().ToUpperInvariant() + "%");
            where.Add($"(UPPER(ITE.ITEM_CODE) LIKE {like} OR UPPER(ITE.ITEM_NAME) LIKE {like})");
        }
        if (codes is { Count: > 0 }) where.Add($"ITE.ITEM_CODE IN ({string.Join(",", codes.Select(ErpXml.Sql))})");
        return "SELECT ITE.*, UNT.UNIT_CODE FROM INVD_ITEM ITE LEFT JOIN INVD_UNIT UNT ON ITE.UNIT_ID = UNT.UNIT_ID"
            + (where.Count > 0 ? " WHERE " + string.Join(" AND ", where) : "")
            + $" ORDER BY ITE.ITEM_CODE LIMIT {(codes is { Count: > 0 } ? codes.Count : 50)}";
    }

    public static ErpEntityDto Entity(IReadOnlyDictionary<string, string?> r) => new(
        Int(r, "ENTITY_ID"), Pick(r, "ENTITY_CODE") ?? "", Pick(r, "ENTITY_NAME") ?? "",
        Pick(r, "TAX_NO", "TAX_NUMBER", "VKN"), Pick(r, "TEL1", "TEL", "PHONE", "TEL_NO"), Pick(r, "EMAIL", "E_MAIL", "MAIL"), null, null);

    public static ErpItemDto Item(IReadOnlyDictionary<string, string?> r) => new(
        Int(r, "ITEM_ID"), Pick(r, "ITEM_CODE") ?? "", Pick(r, "ITEM_NAME", "ITEM_NAME2") ?? "", Pick(r, "UNIT_CODE"), Pick(r, "BRAND_DESC", "BRAND_CODE"), null, null);
}

// ---------------------------------------------------------------- Premium durumu (tüm oturumlar)

public sealed record GetFeaturesQuery : IRequest<FeaturesDto>;

public sealed class GetFeaturesQueryHandler : IRequestHandler<GetFeaturesQuery, FeaturesDto>
{
    private readonly IFeatureFlags _features;
    private readonly IErpSettingsProvider _settings;

    public GetFeaturesQueryHandler(IFeatureFlags features, IErpSettingsProvider settings)
    {
        _features = features;
        _settings = settings;
    }

    public async Task<FeaturesDto> Handle(GetFeaturesQuery request, CancellationToken cancellationToken)
    {
        if (!_features.ErpIntegration) return new FeaturesDto(false, nameof(ErpMode.Off));
        return new FeaturesDto(true, (await _settings.GetAsync(cancellationToken)).Mode.ToString());
    }
}

// ---------------------------------------------------------------- Ayar

public sealed record GetErpSettingsQuery : IRequest<ErpSettingsDto>;

public sealed record UpdateErpSettingsCommand(ErpSettingsRequest Settings) : IRequest;

public sealed record TestErpConnectionCommand(ErpSettingsRequest Settings) : IRequest<ErpTestResultDto>;

public sealed class ErpSettingsRequestValidator : AbstractValidator<ErpSettingsRequest>
{
    public ErpSettingsRequestValidator()
    {
        RuleFor(x => x.Mode).Must(m => Enum.TryParse<ErpMode>(m, out _)).WithMessage("Geçersiz ERP modu.");
        RuleFor(x => x.GetServiceUrl).Must(BeUrlOrEmpty).WithMessage("Okuma servisi adresi geçerli bir http(s) adresi olmalıdır.");
        RuleFor(x => x.SaveServiceUrl).Must(BeUrlOrEmpty).WithMessage("Yazma servisi adresi geçerli bir http(s) adresi olmalıdır.");
        When(x => x.Mode != nameof(ErpMode.Off), () =>
        {
            RuleFor(x => x.GetServiceUrl).NotEmpty().WithMessage("Okuma servisi adresi boş olamaz.");
            RuleFor(x => x.Username).NotEmpty().WithMessage("ERP kullanıcı adı boş olamaz.");
            RuleFor(x => x.CoCode).NotEmpty().WithMessage("ERP şirket kodu boş olamaz.");
        });
        When(x => x.Mode == nameof(ErpMode.Live), () =>
        {
            RuleFor(x => x.SaveServiceUrl).NotEmpty().WithMessage("Canlı modda yazma servisi adresi boş olamaz.");
            RuleFor(x => x.BranchCode).NotEmpty().WithMessage("Canlı modda şube kodu boş olamaz.");
            RuleFor(x => x.OfferDocTraCode).NotEmpty().WithMessage("Canlı modda teklif hareket türü kodu boş olamaz.");
            RuleFor(x => x.OrderDocTraCode).NotEmpty().WithMessage("Canlı modda sipariş hareket türü kodu boş olamaz.");
        });
        RuleFor(x => x.DocNoPrefix).MaximumLength(10);
    }

    private static bool BeUrlOrEmpty(string? url) =>
        string.IsNullOrWhiteSpace(url) || (Uri.TryCreate(url, UriKind.Absolute, out Uri? u) && (u.Scheme == "http" || u.Scheme == "https"));
}

public sealed class UpdateErpSettingsCommandValidator : AbstractValidator<UpdateErpSettingsCommand>
{
    public UpdateErpSettingsCommandValidator() => RuleFor(x => x.Settings).NotNull().SetValidator(new ErpSettingsRequestValidator());
}

public sealed class ErpSettingsHandlers :
    IRequestHandler<GetErpSettingsQuery, ErpSettingsDto>,
    IRequestHandler<UpdateErpSettingsCommand>,
    IRequestHandler<TestErpConnectionCommand, ErpTestResultDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IFeatureFlags _features;
    private readonly ISecretProtector _protector;
    private readonly IErpSettingsProvider _provider;
    private readonly IErpClient _client;

    public ErpSettingsHandlers(IApplicationDbContext context, IFeatureFlags features, ISecretProtector protector, IErpSettingsProvider provider, IErpClient client)
    {
        _context = context;
        _features = features;
        _protector = protector;
        _provider = provider;
        _client = client;
    }

    public async Task<ErpSettingsDto> Handle(GetErpSettingsQuery request, CancellationToken cancellationToken)
    {
        ErpGuard.EnsureLicensed(_features);
        ErpSettings s = await _context.ErpSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken) ?? new ErpSettings();
        return new ErpSettingsDto(s.Mode.ToString(), s.GetServiceUrl, s.SaveServiceUrl, s.Username, !string.IsNullOrEmpty(s.PasswordProtected),
            s.CoCode, s.BranchCode, s.OfferDocTraCode, s.OrderDocTraCode, s.DocNoPrefix, s.EntityCodePrefix, s.ExportNewSuppliers, s.ExportNewProducts,
            s.UpdatedAt ?? (s.CreatedAt == default ? null : s.CreatedAt), s.UpdatedBy ?? s.CreatedBy);
    }

    public async Task Handle(UpdateErpSettingsCommand request, CancellationToken cancellationToken)
    {
        ErpGuard.EnsureLicensed(_features);
        ErpSettingsRequest r = request.Settings;
        ErpSettings? s = await _context.ErpSettings.FirstOrDefaultAsync(cancellationToken);
        if (s is null)
        {
            s = new ErpSettings();
            _context.ErpSettings.Add(s);
        }

        s.Mode = Enum.Parse<ErpMode>(r.Mode);
        s.GetServiceUrl = r.GetServiceUrl.Trim();
        s.SaveServiceUrl = r.SaveServiceUrl.Trim();
        s.Username = r.Username.Trim();
        s.CoCode = r.CoCode.Trim();
        s.BranchCode = r.BranchCode.Trim();
        s.OfferDocTraCode = r.OfferDocTraCode.Trim();
        s.OrderDocTraCode = r.OrderDocTraCode.Trim();
        s.DocNoPrefix = r.DocNoPrefix.Trim();
        s.EntityCodePrefix = string.IsNullOrWhiteSpace(r.EntityCodePrefix) ? null : r.EntityCodePrefix.Trim();
        s.ExportNewSuppliers = r.ExportNewSuppliers;
        s.ExportNewProducts = r.ExportNewProducts;
        if (!string.IsNullOrEmpty(r.Password)) s.PasswordProtected = _protector.Protect(r.Password);

        await _context.SaveChangesAsync(cancellationToken);
        _provider.Invalidate();
    }

    /// <summary>Formdaki (kaydedilmemiş olabilir) bağlantıyla yalnızca okuma yapar: cari ve stok kartı sayısı.</summary>
    public async Task<ErpTestResultDto> Handle(TestErpConnectionCommand request, CancellationToken cancellationToken)
    {
        ErpGuard.EnsureLicensed(_features);
        ErpSettingsRequest r = request.Settings;
        string? password = r.Password;
        if (string.IsNullOrEmpty(password))
        {
            string? stored = await _context.ErpSettings.AsNoTracking().Select(x => x.PasswordProtected).FirstOrDefaultAsync(cancellationToken);
            password = stored is null ? null : _protector.Unprotect(stored);
        }

        var connection = new ErpConnection(ErpMode.Simulation, r.GetServiceUrl.Trim(), r.SaveServiceUrl.Trim(), r.Username.Trim(), password,
            r.CoCode.Trim(), r.BranchCode.Trim(), "", "", "", r.EntityCodePrefix, false, false);
        if (!connection.CanRead) return new ErpTestResultDto(false, "Okuma servisi adresi ve kullanıcı adı gerekli.", null, null);

        try
        {
            var entities = await _client.QueryAsync(connection, "SELECT COUNT(*) AS N FROM FIND_ENTITY", cancellationToken);
            var items = await _client.QueryAsync(connection, "SELECT COUNT(*) AS N FROM INVD_ITEM", cancellationToken);
            return new ErpTestResultDto(true, null,
                entities.Count > 0 ? ErpRows.Int(entities[0], "N") : 0, items.Count > 0 ? ErpRows.Int(items[0], "N") : 0);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            return new ErpTestResultDto(false, ex.Message, null, null);
        }
    }
}

// ---------------------------------------------------------------- Cari / ürün aktarımı (ERP → ProcureLive)

public sealed record SearchErpEntitiesQuery(string? Search) : IRequest<IReadOnlyList<ErpEntityDto>>;

public sealed record ImportErpEntitiesCommand(ErpImportRequest Request) : IRequest<ErpImportResultDto>;

public sealed record SearchErpItemsQuery(string? Search) : IRequest<IReadOnlyList<ErpItemDto>>;

public sealed record ImportErpItemsCommand(ErpImportRequest Request) : IRequest<ErpImportResultDto>;

public sealed class ImportErpItemsCommandValidator : AbstractValidator<ImportErpItemsCommand>
{
    public ImportErpItemsCommandValidator()
    {
        RuleFor(x => x.Request.Codes).NotEmpty().WithMessage("Aktarılacak kayıt seçin.");
        RuleFor(x => x.Request.CategoryId).NotNull().WithMessage("Aktarılan ürünler için kategori seçin.");
    }
}

public sealed class ImportErpEntitiesCommandValidator : AbstractValidator<ImportErpEntitiesCommand>
{
    public ImportErpEntitiesCommandValidator() => RuleFor(x => x.Request.Codes).NotEmpty().WithMessage("Aktarılacak kayıt seçin.");
}

public sealed class ErpImportHandlers :
    IRequestHandler<SearchErpEntitiesQuery, IReadOnlyList<ErpEntityDto>>,
    IRequestHandler<ImportErpEntitiesCommand, ErpImportResultDto>,
    IRequestHandler<SearchErpItemsQuery, IReadOnlyList<ErpItemDto>>,
    IRequestHandler<ImportErpItemsCommand, ErpImportResultDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IFeatureFlags _features;
    private readonly IErpSettingsProvider _provider;
    private readonly IErpClient _client;

    public ErpImportHandlers(IApplicationDbContext context, IFeatureFlags features, IErpSettingsProvider provider, IErpClient client)
    {
        _context = context;
        _features = features;
        _provider = provider;
        _client = client;
    }

    public async Task<IReadOnlyList<ErpEntityDto>> Handle(SearchErpEntitiesQuery request, CancellationToken cancellationToken)
    {
        ErpConnection c = await ErpGuard.RequireEnabledAsync(_features, _provider, cancellationToken);
        List<ErpEntityDto> rows = (await Query(c, ErpRows.EntitySql(c, request.Search, null), cancellationToken)).Select(ErpRows.Entity).ToList();

        List<string> codes = rows.Select(r => r.Code).ToList();
        var linked = await _context.Companies.AsNoTracking()
            .Where(x => x.ErpEntityCode != null && codes.Contains(x.ErpEntityCode))
            .Select(x => new { x.ErpEntityCode, x.Id, x.Name }).ToListAsync(cancellationToken);
        return rows.Select(r => linked.FirstOrDefault(l => l.ErpEntityCode == r.Code) is { } l ? r with { LinkedCompanyId = l.Id, LinkedCompanyName = l.Name } : r).ToList();
    }

    /// <summary>
    /// Seçilen ERP carilerini tedarikçi olarak aktarır: ERP kodu, vergi numarası ya da unvanla eşleşen cari güncellenir — yalnızca
    /// ERP eşlemesi ve boş alanlar; ProcureLive'da düzenlenmiş ad korunur. Yeni cari ERP'den geldiği için doğrudan Aktif açılır.
    /// </summary>
    public async Task<ErpImportResultDto> Handle(ImportErpEntitiesCommand request, CancellationToken cancellationToken)
    {
        ErpConnection c = await ErpGuard.RequireEnabledAsync(_features, _provider, cancellationToken);
        List<ErpEntityDto> rows = (await Query(c, ErpRows.EntitySql(c, null, request.Request.Codes), cancellationToken)).Select(ErpRows.Entity).ToList();

        int created = 0, updated = 0;
        var errors = new List<string>();
        foreach (string missing in request.Request.Codes.Except(rows.Select(r => r.Code)))
            errors.Add($"{missing}: ERP'de bulunamadı.");

        foreach (ErpEntityDto row in rows)
        {
            // Eşleme sırası: ERP kodu → vergi no → unvan (büyük/küçük harf duyarsız). Böylece ERP'den önce elle açılmış cari kopyalanmaz.
            string name = row.Name.Trim().ToLower();
            Company? company = await _context.Companies.FirstOrDefaultAsync(x => x.ErpEntityCode == row.Code, cancellationToken)
                ?? (string.IsNullOrWhiteSpace(row.TaxNo) ? null
                    : await _context.Companies.FirstOrDefaultAsync(x => x.CompanyType == CompanyType.Supplier && x.TaxNumber == row.TaxNo, cancellationToken))
                ?? await _context.Companies.FirstOrDefaultAsync(x => x.CompanyType == CompanyType.Supplier && x.ErpEntityCode == null && x.Name.ToLower() == name, cancellationToken);

            if (company is null)
            {
                company = new Company
                {
                    Code = await CompanyRules.NextCodeAsync(_context, cancellationToken),
                    Name = row.Name,
                    CompanyType = CompanyType.Supplier,
                    Status = CompanyStatus.Active,
                    WorkspaceId = request.Request.WorkspaceId
                };
                _context.Companies.Add(company);
                created++;
            }
            else
            {
                if (company.CompanyType != CompanyType.Supplier)
                {
                    errors.Add($"{row.Code}: eşleşen kayıt bir firma (iç şirket), tedarikçi değil.");
                    continue;
                }
                updated++;
            }

            company.ErpEntityCode = row.Code;
            company.ErpEntityId = row.Id == 0 ? company.ErpEntityId : row.Id;
            company.TaxNumber ??= row.TaxNo;
            company.Phone ??= row.Phone;
            company.Email ??= row.Email;
            // NextCodeAsync kaydedilmemiş kodları görmez: her yeni cariden sonra kaydet.
            await _context.SaveChangesAsync(cancellationToken);
        }

        return new ErpImportResultDto(created, updated, errors);
    }

    public async Task<IReadOnlyList<ErpItemDto>> Handle(SearchErpItemsQuery request, CancellationToken cancellationToken)
    {
        ErpConnection c = await ErpGuard.RequireEnabledAsync(_features, _provider, cancellationToken);
        List<ErpItemDto> rows = (await Query(c, ErpRows.ItemSql(request.Search, null), cancellationToken)).Select(ErpRows.Item).ToList();

        List<string> codes = rows.Select(r => r.Code).ToList();
        var linked = await _context.Products.AsNoTracking()
            .Where(x => (x.ErpItemCode != null && codes.Contains(x.ErpItemCode)))
            .Select(x => new { x.ErpItemCode, x.Id, x.Name }).ToListAsync(cancellationToken);
        return rows.Select(r => linked.FirstOrDefault(l => l.ErpItemCode == r.Code) is { } l ? r with { LinkedProductId = l.Id, LinkedProductName = l.Name } : r).ToList();
    }

    /// <summary>Seçilen ERP stok kartlarını ürün olarak aktarır: ERP koduyla, yoksa aynı stok koduyla (SKU) eşleşen ürün eşlenir.</summary>
    public async Task<ErpImportResultDto> Handle(ImportErpItemsCommand request, CancellationToken cancellationToken)
    {
        ErpConnection c = await ErpGuard.RequireEnabledAsync(_features, _provider, cancellationToken);
        Guid categoryId = request.Request.CategoryId!.Value;
        if (!await _context.Categories.AnyAsync(x => x.Id == categoryId, cancellationToken))
            throw new NotFoundException(nameof(Category), categoryId);

        List<ErpItemDto> rows = (await Query(c, ErpRows.ItemSql(null, request.Request.Codes), cancellationToken)).Select(ErpRows.Item).ToList();
        int created = 0, updated = 0;
        var errors = new List<string>();
        foreach (string missing in request.Request.Codes.Except(rows.Select(r => r.Code)))
            errors.Add($"{missing}: ERP'de bulunamadı.");

        foreach (ErpItemDto row in rows)
        {
            Product? product = await _context.Products.FirstOrDefaultAsync(x => x.ErpItemCode == row.Code, cancellationToken)
                ?? await _context.Products.FirstOrDefaultAsync(x => x.Sku == row.Code, cancellationToken);

            if (product is null)
            {
                if (await _context.Products.IgnoreQueryFilters().AnyAsync(x => x.Sku == row.Code, cancellationToken))
                {
                    errors.Add($"{row.Code}: bu stok kodu silinmiş bir üründe kullanılıyor.");
                    continue;
                }

                product = new Product
                {
                    Sku = row.Code,
                    Name = row.Name,
                    CategoryId = categoryId,
                    UnitOfMeasure = ErpUnits.Parse(row.UnitCode),
                    Brand = row.Brand,
                    WorkspaceId = request.Request.WorkspaceId,
                    IsActive = true
                };
                _context.Products.Add(product);
                created++;
            }
            else
            {
                updated++;
            }

            product.ErpItemCode = row.Code;
            product.ErpItemId = row.Id == 0 ? product.ErpItemId : row.Id;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return new ErpImportResultDto(created, updated, errors);
    }

    private async Task<IReadOnlyList<IReadOnlyDictionary<string, string?>>> Query(ErpConnection c, string sql, CancellationToken ct)
    {
        try
        {
            return await _client.QueryAsync(c, sql, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException && ex is not ConflictException)
        {
            throw new ConflictException($"ERP'den okunamadı: {ex.Message}");
        }
    }
}

// ---------------------------------------------------------------- ProcureLive → ERP (elle başlatılan işler)

public sealed record ExportCompanyToErpCommand(Guid CompanyId) : IRequest;

public sealed record ExportProductToErpCommand(Guid ProductId) : IRequest;

/// <summary>"ERP'de sipariş oluştur": gönderilmiş/onaylanmış satınalma siparişini ERP siparişi olarak kuyruğa alır.</summary>
[RequiresPermission(PermissionCodes.OrdersManage)]
public sealed record CreateErpOrderCommand(Guid PurchaseOrderId) : IRequest;

public sealed record RetryErpJobCommand(Guid JobId) : IRequest;

public sealed class ErpActionHandlers :
    IRequestHandler<ExportCompanyToErpCommand>,
    IRequestHandler<ExportProductToErpCommand>,
    IRequestHandler<CreateErpOrderCommand>,
    IRequestHandler<RetryErpJobCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IFeatureFlags _features;
    private readonly IErpSettingsProvider _provider;
    private readonly IDateTime _dateTime;

    public ErpActionHandlers(IApplicationDbContext context, IFeatureFlags features, IErpSettingsProvider provider, IDateTime dateTime)
    {
        _context = context;
        _features = features;
        _provider = provider;
        _dateTime = dateTime;
    }

    public async Task Handle(ExportCompanyToErpCommand request, CancellationToken cancellationToken)
    {
        await ErpGuard.RequireEnabledAsync(_features, _provider, cancellationToken);
        Company company = await _context.Companies.FirstOrDefaultAsync(x => x.Id == request.CompanyId, cancellationToken)
            ?? throw new NotFoundException(nameof(Company), request.CompanyId);
        if (company.CompanyType != CompanyType.Supplier) throw new ConflictException("Yalnızca tedarikçi cariler ERP'ye gönderilir.");
        await Enqueue(ErpSyncKind.EntityExport, $"Cari · {company.Code} {company.Name}", j => j.CompanyId = company.Id,
            j => j.CompanyId == company.Id, cancellationToken);
    }

    public async Task Handle(ExportProductToErpCommand request, CancellationToken cancellationToken)
    {
        await ErpGuard.RequireEnabledAsync(_features, _provider, cancellationToken);
        Product product = await _context.Products.FirstOrDefaultAsync(x => x.Id == request.ProductId, cancellationToken)
            ?? throw new NotFoundException(nameof(Product), request.ProductId);
        await Enqueue(ErpSyncKind.ItemExport, $"Ürün · {product.Sku} {product.Name}", j => j.ProductId = product.Id,
            j => j.ProductId == product.Id, cancellationToken);
    }

    public async Task Handle(CreateErpOrderCommand request, CancellationToken cancellationToken)
    {
        await ErpGuard.RequireEnabledAsync(_features, _provider, cancellationToken);
        var po = await _context.PurchaseOrders.AsNoTracking().Where(x => x.Id == request.PurchaseOrderId)
            .Select(x => new { x.Id, x.OrderNumber, x.Status, x.ErpOrderDocNo, Supplier = x.SupplierCompany.Name })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("PurchaseOrder", request.PurchaseOrderId);
        if (po.ErpOrderDocNo is not null) throw new ConflictException($"Bu sipariş ERP'de zaten oluşturuldu ({po.ErpOrderDocNo}).");
        if (po.Status is not (PurchaseOrderStatus.Sent or PurchaseOrderStatus.Confirmed))
            throw new ConflictException("ERP siparişi yalnızca tedarikçiye gönderilmiş veya onaylanmış siparişten oluşturulur.");
        await Enqueue(ErpSyncKind.OrderCreate, $"Sipariş · {po.OrderNumber} · {po.Supplier}", j => j.PurchaseOrderId = po.Id,
            j => j.PurchaseOrderId == po.Id, cancellationToken);
    }

    public async Task Handle(RetryErpJobCommand request, CancellationToken cancellationToken)
    {
        ErpGuard.EnsureLicensed(_features);
        ErpSyncJob job = await _context.ErpSyncJobs.FirstOrDefaultAsync(x => x.Id == request.JobId, cancellationToken)
            ?? throw new NotFoundException(nameof(ErpSyncJob), request.JobId);
        if (job.Status is not (ErpSyncStatus.Failed or ErpSyncStatus.Skipped or ErpSyncStatus.Simulated))
            throw new ConflictException("Yalnızca başarısız, atlanmış veya simüle edilmiş işler yeniden çalıştırılabilir.");
        job.Status = ErpSyncStatus.Pending;
        job.AttemptCount = 0;
        job.NextAttemptAt = null;
        job.Message = null;
        job.CompletedAt = null;
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Aynı kayıt için bekleyen iş varsa yenisi açılmaz (gönderim anında güncel veri kullanılır).</summary>
    private async Task Enqueue(ErpSyncKind kind, string summary, Action<ErpSyncJob> target,
        System.Linq.Expressions.Expression<Func<ErpSyncJob, bool>> sameTarget, CancellationToken ct)
    {
        if (await _context.ErpSyncJobs.Where(j => j.Kind == kind && j.Status == ErpSyncStatus.Pending).AnyAsync(sameTarget, ct))
            return;
        var job = new ErpSyncJob { Kind = kind, Summary = summary, CreatedAt = _dateTime.UtcNow };
        target(job);
        _context.ErpSyncJobs.Add(job);
        await _context.SaveChangesAsync(ct);
    }
}

// ---------------------------------------------------------------- Kayıtlar ve kart durumu

public sealed record GetErpJobsQuery(string? Status, string? Kind, string? Search, int Page, int PageSize) : IRequest<PagedResult<ErpSyncJobDto>>;

public sealed record GetErpJobSummaryQuery : IRequest<ErpSyncSummaryDto>;

public sealed record GetErpJobQuery(Guid JobId) : IRequest<ErpSyncJobDetailDto>;

public sealed record GetErpRecordStatusQuery(Guid? CompanyId, Guid? ProductId, Guid? PurchaseOrderId, Guid? AuctionId) : IRequest<ErpRecordStatusDto>;

public sealed class ErpQueryHandlers :
    IRequestHandler<GetErpJobsQuery, PagedResult<ErpSyncJobDto>>,
    IRequestHandler<GetErpJobSummaryQuery, ErpSyncSummaryDto>,
    IRequestHandler<GetErpJobQuery, ErpSyncJobDetailDto>,
    IRequestHandler<GetErpRecordStatusQuery, ErpRecordStatusDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IFeatureFlags _features;
    private readonly IErpSettingsProvider _provider;

    public ErpQueryHandlers(IApplicationDbContext context, IFeatureFlags features, IErpSettingsProvider provider)
    {
        _context = context;
        _features = features;
        _provider = provider;
    }

    public async Task<PagedResult<ErpSyncJobDto>> Handle(GetErpJobsQuery request, CancellationToken cancellationToken)
    {
        ErpGuard.EnsureLicensed(_features);
        int page = Math.Max(1, request.Page), size = Math.Clamp(request.PageSize, 1, 100);
        IQueryable<ErpSyncJob> q = _context.ErpSyncJobs.AsNoTracking();
        if (Enum.TryParse(request.Status, out ErpSyncStatus status)) q = q.Where(j => j.Status == status);
        if (Enum.TryParse(request.Kind, out ErpSyncKind kind)) q = q.Where(j => j.Kind == kind);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            string term = request.Search.Trim().ToLowerInvariant();
            q = q.Where(j => j.Summary.ToLower().Contains(term));
        }

        int total = await q.CountAsync(cancellationToken);
        List<ErpSyncJob> items = await q.OrderByDescending(j => j.CreatedAt).Skip((page - 1) * size).Take(size).ToListAsync(cancellationToken);
        return new PagedResult<ErpSyncJobDto>(items.Select(ToDto).ToList(), total, page, size);
    }

    public async Task<ErpSyncSummaryDto> Handle(GetErpJobSummaryQuery request, CancellationToken cancellationToken)
    {
        ErpGuard.EnsureLicensed(_features);
        var counts = await _context.ErpSyncJobs.AsNoTracking().GroupBy(j => j.Status)
            .Select(g => new { g.Key, N = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.N, cancellationToken);
        int Get(ErpSyncStatus s) => counts.TryGetValue(s, out int n) ? n : 0;
        return new ErpSyncSummaryDto(Get(ErpSyncStatus.Pending), Get(ErpSyncStatus.Simulated), Get(ErpSyncStatus.Done), Get(ErpSyncStatus.Failed), Get(ErpSyncStatus.Skipped));
    }

    public async Task<ErpSyncJobDetailDto> Handle(GetErpJobQuery request, CancellationToken cancellationToken)
    {
        ErpGuard.EnsureLicensed(_features);
        ErpSyncJob job = await _context.ErpSyncJobs.AsNoTracking().FirstOrDefaultAsync(j => j.Id == request.JobId, cancellationToken)
            ?? throw new NotFoundException(nameof(ErpSyncJob), request.JobId);
        return new ErpSyncJobDetailDto(ToDto(job), job.RequestXml, job.ResponseText);
    }

    public async Task<ErpRecordStatusDto> Handle(GetErpRecordStatusQuery request, CancellationToken cancellationToken)
    {
        if (!_features.ErpIntegration) return new ErpRecordStatusDto(false, nameof(ErpMode.Off), null, [], []);
        ErpConnection c = await _provider.GetAsync(cancellationToken);

        string? code = null;
        if (request.CompanyId is Guid companyId)
            code = await _context.Companies.Where(x => x.Id == companyId).Select(x => x.ErpEntityCode).FirstOrDefaultAsync(cancellationToken);
        else if (request.ProductId is Guid productId)
            code = await _context.Products.Where(x => x.Id == productId).Select(x => x.ErpItemCode).FirstOrDefaultAsync(cancellationToken);
        else if (request.PurchaseOrderId is Guid poId)
            code = await _context.PurchaseOrders.Where(x => x.Id == poId).Select(x => x.ErpOrderDocNo).FirstOrDefaultAsync(cancellationToken);

        IQueryable<ErpSyncJob> q = _context.ErpSyncJobs.AsNoTracking();
        q = request.CompanyId is Guid cid ? q.Where(j => j.CompanyId == cid && j.Kind == ErpSyncKind.EntityExport)
            : request.ProductId is Guid pid ? q.Where(j => j.ProductId == pid)
            : request.PurchaseOrderId is Guid oid ? q.Where(j => j.PurchaseOrderId == oid)
            : request.AuctionId is Guid aid ? q.Where(j => j.AuctionId == aid)
            : q.Where(_ => false);
        List<ErpSyncJob> jobs = await q.OrderByDescending(j => j.CreatedAt).Take(request.AuctionId is null ? 5 : 30).ToListAsync(cancellationToken);

        var offers = new List<ErpOfferLinkDto>();
        if (request.AuctionId is Guid auctionId)
        {
            var links = await _context.ErpOfferLinks.AsNoTracking().Where(l => l.AuctionId == auctionId)
                .Join(_context.Companies.IgnoreQueryFilters(), l => l.CompanyId, co => co.Id, (l, co) => new { l, co.Name })
                .OrderBy(x => x.Name).ToListAsync(cancellationToken);
            foreach (var x in links)
            {
                ErpSyncJob? last = jobs.FirstOrDefault(j => j.CompanyId == x.l.CompanyId);
                offers.Add(new ErpOfferLinkDto(x.l.CompanyId, x.Name, x.l.DocNo, x.l.ErpOfferMId, x.l.CreatedAt, x.l.LastRevisedBidAt,
                    last?.Status.ToString(), last?.Message));
            }
        }

        return new ErpRecordStatusDto(true, c.Mode.ToString(), code, jobs.Select(ToDto).ToList(), offers);
    }

    internal static ErpSyncJobDto ToDto(ErpSyncJob j) => new(j.Id, j.Kind.ToString(), j.Status.ToString(), j.Summary, j.Message, j.AttemptCount,
        j.CreatedAt, j.CompletedAt, j.AuctionId, j.CompanyId, j.ProductId, j.PurchaseOrderId);
}
