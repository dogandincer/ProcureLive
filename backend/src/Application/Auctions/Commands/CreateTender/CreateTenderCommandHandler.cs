using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Workspaces;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Application.Common.Security;
using ProcureLive.Domain.Entities.Auctions;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Auctions.Commands.CreateTender;

/// <summary>
/// Yüksek hacimli ihale oluşturma (ör. 30 kalem × 40 cari). Tüm alt kayıtlar Auction'ın
/// navigation koleksiyonlarına eklenip TEK SaveChangesAsync ile yazılır:
///   - EF Core tek SaveChanges'i tek bir transaction içinde çalıştırır → atomiktir (ya hep ya hiç).
///   - Navigation koleksiyonu kullanıldığı için EF önce parent'ı INSERT edip üretilen Id'yi
///     çocuklara otomatik dağıtır; Npgsql bu INSERT'leri tek round-trip'te batch'ler (N+1 yok).
///   - Referans doğrulamaları kalem/cari başına DEĞİL, tipe göre TEK sorguda toplu yapılır.
/// Not: ~70-80 satırlık bu hacim için EF'in varsayılan batch'i optimaldir. Binlerce satıra
/// çıkıldığında ChangeTracker.AutoDetectChangesEnabled=false veya EFCore.BulkExtensions/COPY
/// değerlendirilmelidir (IApplicationDbContext arkasında ChangeTracker'a erişim yoktur).
/// </summary>
public sealed class CreateTenderCommandHandler : IRequestHandler<CreateTenderCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTime _dateTime;
    private readonly IDataScopeService _scope;

    public CreateTenderCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTime dateTime,
        IDataScopeService scope)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTime = dateTime;
        _scope = scope;
    }

    public async Task<Guid> Handle(CreateTenderCommand request, CancellationToken cancellationToken)
    {
        DateTime now = _dateTime.UtcNow;

        // --- Toplu referans doğrulama (tipe göre tek sorgu; kalem/cari başına sorgu YOK) ---
        bool currencyExists = await _context.Currencies
            .AnyAsync(c => c.Id == request.CurrencyId, cancellationToken);
        if (!currencyExists)
            throw new NotFoundException("Currency", request.CurrencyId);

        bool ownerExists = await _context.Companies
            .AnyAsync(c => c.Id == request.OwnerCompanyId, cancellationToken);
        if (!ownerExists)
            throw new NotFoundException("Company", request.OwnerCompanyId);

        // Benzersiz cari id'leri (payload'da tekrar gelmiş olabilir)
        List<Guid> vendorIds = request.VendorIds.Distinct().ToList();
        List<Guid> existingVendorIds = await _context.Companies
            .Where(c => vendorIds.Contains(c.Id))
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);
        if (existingVendorIds.Count != vendorIds.Count)
        {
            IEnumerable<Guid> missing = vendorIds.Except(existingVendorIds);
            throw new ConflictException($"Bulunamayan tedarikçi(ler): {string.Join(", ", missing)}");
        }

        // Kalemlerdeki ürün id'leri (varsa) toplu doğrulama
        List<Guid> productIds = request.Lines
            .Where(l => l.ProductId.HasValue)
            .Select(l => l.ProductId!.Value)
            .Distinct()
            .ToList();
        if (productIds.Count > 0)
        {
            List<Guid> existingProductIds = await _context.Products
                .Where(p => productIds.Contains(p.Id))
                .Select(p => p.Id)
                .ToListAsync(cancellationToken);
            if (existingProductIds.Count != productIds.Count)
            {
                IEnumerable<Guid> missing = productIds.Except(existingProductIds);
                throw new ConflictException($"Bulunamayan ürün(ler): {string.Join(", ", missing)}");
            }
        }

        // İhalenin işyeri (katılım taleplerinde cari onayı bu işyerine verilir) ve sorumlu satınalmacı (açan kullanıcı).
        Guid? workspaceId = await FirmWorkspaceRules.ResolveAsync(
            _context, request.OwnerCompanyId, request.WorkspaceId, await _scope.GetAsync(cancellationToken), cancellationToken);

        // --- Aggregate'i navigation koleksiyonlarıyla kur (tek SaveChanges ile yazılır) ---
        Guid invitedByUserId = _currentUserService.UserId ?? Guid.Empty;

        var auction = new Auction
        {
            ReferenceCode = string.IsNullOrWhiteSpace(request.ReferenceCode)
                ? GenerateReferenceCode(now)
                : request.ReferenceCode.Trim(),
            Title = request.Title,
            Description = request.Description,
            AuctionType = request.AuctionType,
            Status = AuctionStatus.Draft,
            OwnerCompanyId = request.OwnerCompanyId,
            WorkspaceId = workspaceId,
            ResponsibleUserId = _currentUserService.UserId,
            CurrencyId = request.CurrencyId,
            StartAt = request.StartAt,
            EndAt = request.EndAt,
            ReservePrice = request.ReservePrice,
            ExtensionRuleMinutes = request.ExtensionTriggerMinutes,
            ExtensionDurationMinutes = request.ExtensionDurationMinutes,
            MaxExtensionMinutes = request.MaxExtensionMinutes,
            PaymentTerm = request.PaymentTerm,
            DeliveryTerm = request.DeliveryTerm,
            AllowAlternativeProducts = request.AllowAlternativeProducts,
            AllowPartialBids = request.AllowPartialBids,
            IsListedForPlus = request.IsListedForPlus,
        };

        foreach (TenderLineInput line in request.Lines)
        {
            auction.Items.Add(new AuctionItem
            {
                ProductId = line.ProductId,
                Description = line.Description,
                Quantity = line.Quantity,
                UnitOfMeasure = line.UnitOfMeasure,
                TargetPrice = line.TargetPrice,
                RequestedDeliveryDate = line.RequestedDeliveryDate,
                Specification = line.Specification,
                SortOrder = line.SortOrder,
            });
        }

        foreach (Guid vendorId in vendorIds)
        {
            auction.Invitations.Add(new AuctionInvitation
            {
                CompanyId = vendorId,
                InvitedByUserId = invitedByUserId,
                Status = InvitationStatus.Invited,
                InvitedAt = now,
            });
        }

        foreach (TenderAttachmentInput att in request.Attachments)
        {
            auction.Attachments.Add(new AuctionAttachment
            {
                FileName = att.FileName,
                Url = att.Url,
                ContentType = att.ContentType,
                SizeBytes = att.SizeBytes,
            });
        }

        _context.Auctions.Add(auction);

        // Tek round-trip, tek transaction: parent + tüm çocuklar atomik insert edilir.
        // Taslak olarak oluşturulur; tedarikçi bildirimleri yayınlama (Publish) adımında gönderilir.
        await _context.SaveChangesAsync(cancellationToken);

        return auction.Id;
    }

    private static string GenerateReferenceCode(DateTime now) =>
        $"IH-{now:yyyy}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";
}
