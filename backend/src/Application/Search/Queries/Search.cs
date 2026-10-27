using MediatR;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Auctions.Status;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Application.Common.Privacy;
using ProcureLive.Application.Common.Security;
using ProcureLive.Contracts.Search;
using ProcureLive.Domain.Entities.Auctions;
using ProcureLive.Domain.Entities.Catalog;
using ProcureLive.Domain.Entities.Companies;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Search.Queries;

public static class SearchScopes
{
    public const string All = "all";
    public const string Companies = "companies";
    public const string Tenders = "tenders";
    public const string Products = "products";
}

public static class TenderSorts
{
    public const string EndingSoon = "endingSoon";
    public const string Newest = "newest";
    public const string ParticipantsDesc = "participantsDesc";
    public const string ParticipantsAsc = "participantsAsc";
}

/// <summary>
/// Plus detaylı arama (şirket / ihale / ürün). Standart üyeye 403 + PlusRequired döner; iç kullanıcılar
/// (Admin/Buyer) Plus vitrinini önizleyebilir. İhale sonuçları, SupplierAccessContext.IsVisible kuralının açık
/// (Scheduled/Live) ihalelerle sınırlı halidir — listelemeye kapalı ihale yalnızca davet/doğrudan ekleme ile görünür.
/// </summary>
public sealed record SearchQuery(
    string? Term,
    string? Scope,
    AuctionType? AuctionType,
    int? MinParticipants,
    int? MaxParticipants,
    Guid? ProductId,
    Guid? OwnerCompanyId,
    string? Sort,
    int Take = 20) : IRequest<SearchResultDto>;

public sealed class SearchQueryHandler : IRequestHandler<SearchQuery, SearchResultDto>
{
    private const int MaxTake = 50;

    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly ISupplierAccessService _accessService;

    public SearchQueryHandler(
        IApplicationDbContext context, ICurrentUserService currentUserService, ISupplierAccessService accessService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _accessService = accessService;
    }

    public async Task<SearchResultDto> Handle(SearchQuery request, CancellationToken cancellationToken)
    {
        bool isInternalUser = _currentUserService.IsInRole(Roles.Admin) || _currentUserService.IsInRole(Roles.Buyer);
        SupplierAccessContext? access = null;

        if (!isInternalUser)
        {
            Guid companyId = _currentUserService.CompanyId
                ?? throw new ForbiddenAccessException("Detaylı arama yalnızca Plus üyelere açıktır.", ErrorCodes.PlusRequired);

            access = await _accessService.GetContextAsync(companyId, cancellationToken);
            if (!access.IsPlus)
                throw new ForbiddenAccessException("Detaylı arama yalnızca Plus üyelere açıktır.", ErrorCodes.PlusRequired);
        }

        string? term = Normalize(request.Term);
        int take = Math.Clamp(request.Take, 1, MaxTake);
        string scope = string.IsNullOrWhiteSpace(request.Scope) ? SearchScopes.All : request.Scope.Trim().ToLowerInvariant();
        bool Wants(string s) => scope == SearchScopes.All || scope == s;

        IQueryable<Auction> openTenders = VisibleOpenTenders(access);

        IReadOnlyList<CompanySearchItemDto> companies = Wants(SearchScopes.Companies)
            ? await SearchCompaniesAsync(term, take, access, openTenders, cancellationToken)
            : Array.Empty<CompanySearchItemDto>();

        IReadOnlyList<TenderSearchItemDto> tenders = Wants(SearchScopes.Tenders)
            ? await SearchTendersAsync(request, term, take, access, openTenders, cancellationToken)
            : Array.Empty<TenderSearchItemDto>();

        IReadOnlyList<ProductSearchItemDto> products = Wants(SearchScopes.Products)
            ? await SearchProductsAsync(term, take, access, openTenders, cancellationToken)
            : Array.Empty<ProductSearchItemDto>();

        return new SearchResultDto(companies, tenders, products);
    }

    /// <summary>Arayanın görebildiği açık ihaleler (SQL'e çevrilebilir). İç kullanıcı: yalnızca Plus vitrini.</summary>
    private IQueryable<Auction> VisibleOpenTenders(SupplierAccessContext? access)
    {
        AuctionStatus[] openStatuses = AuctionStatusMachine.OpenStatuses;
        IQueryable<Auction> open = _context.Auctions.Where(a => openStatuses.Contains(a.Status));

        if (access is null)
        {
            return open.Where(a => a.IsListedForPlus && a.OwnerCompany.ListTendersForPlus);
        }

        List<Guid> directIds = access.InvitedAuctionIds.Union(access.JoinedAuctionIds).ToList();
        List<Guid> ownerIds = access.InvitingOwnerIds.ToList();
        List<Guid> declinedIds = access.DeclinedAuctionIds.ToList();

        return open.Where(a => !declinedIds.Contains(a.Id)
            && (directIds.Contains(a.Id)
                || ownerIds.Contains(a.OwnerCompanyId)
                || (a.IsListedForPlus && a.OwnerCompany.ListTendersForPlus)));
    }

    private async Task<IReadOnlyList<CompanySearchItemDto>> SearchCompaniesAsync(
        string? term, int take, SupplierAccessContext? access, IQueryable<Auction> openTenders, CancellationToken ct)
    {
        IQueryable<Company> query = _context.Companies.Where(c => c.Status == CompanyStatus.Active);

        if (term is not null)
        {
            // ToLower + ı→i: Türkçe büyük/küçük harf ve noktalı/noktasız i farkı aramayı bozmasın.
            query = query.Where(c =>
                c.Name.ToLower().Replace("ı", "i").Contains(term)
                || (c.TradeName != null && c.TradeName.ToLower().Replace("ı", "i").Contains(term))
                || c.Sectors.Any(s => s.Sector.Name.ToLower().Replace("ı", "i").Contains(term))
                || c.Addresses.Any(a => a.City.ToLower().Replace("ı", "i").Contains(term)
                    || (a.District != null && a.District.ToLower().Replace("ı", "i").Contains(term))));
        }

        var rows = await query
            .OrderBy(c => c.Name)
            .Take(take)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.TradeName,
                c.LogoUrl,
                c.CompanyType,
                c.Email,
                c.Phone,
                c.Website,
                c.ShowContactInfo,
                Address = c.Addresses.OrderBy(a => a.AddressType).Select(a => new { a.District, a.City }).FirstOrDefault(),
                Sectors = c.Sectors.Select(s => s.Sector.Name).ToList(),
                AverageScore = c.Evaluations.Average(e => (decimal?)e.OverallScore),
            })
            .ToListAsync(ct);

        List<Guid> ids = rows.Select(r => r.Id).ToList();
        Dictionary<Guid, int> tenderCounts = (await openTenders
                .Where(a => ids.Contains(a.OwnerCompanyId))
                .Select(a => a.OwnerCompanyId)
                .ToListAsync(ct))
            .GroupBy(id => id)
            .ToDictionary(g => g.Key, g => g.Count());

        return rows.Select(r =>
        {
            // İç kullanıcı ve firmanın kendisi ham veriyi görür; diğerleri için izin yoksa sunucuda maskelenir.
            bool reveal = access is null || r.ShowContactInfo || r.Id == access.CompanyId;

            return new CompanySearchItemDto(
                r.Id,
                r.Name,
                r.TradeName,
                r.LogoUrl,
                r.CompanyType.ToString(),
                ContactMasker.ShortAddress(r.Address?.District, r.Address?.City),
                r.Sectors,
                r.AverageScore is decimal score ? Math.Round(score, 2) : (decimal?)null,
                reveal ? r.Email : ContactMasker.MaskEmail(r.Email),
                reveal ? r.Phone : ContactMasker.MaskPhone(r.Phone),
                reveal ? r.Website : null,
                !reveal,
                tenderCounts.GetValueOrDefault(r.Id));
        }).ToList();
    }

    private static async Task<IReadOnlyList<TenderSearchItemDto>> SearchTendersAsync(
        SearchQuery request, string? term, int take, SupplierAccessContext? access, IQueryable<Auction> openTenders,
        CancellationToken ct)
    {
        IQueryable<Auction> query = openTenders;

        if (term is not null)
        {
            query = query.Where(a =>
                a.Title.ToLower().Replace("ı", "i").Contains(term)
                || (a.ReferenceCode != null && a.ReferenceCode.ToLower().Contains(term))
                || a.OwnerCompany.Name.ToLower().Replace("ı", "i").Contains(term)
                || a.Items.Any(i => i.Description.ToLower().Replace("ı", "i").Contains(term)));
        }

        if (request.AuctionType is AuctionType auctionType)
            query = query.Where(a => a.AuctionType == auctionType);
        if (request.ProductId is Guid productId)
            query = query.Where(a => a.Items.Any(i => i.ProductId == productId));
        if (request.OwnerCompanyId is Guid ownerId)
            query = query.Where(a => a.OwnerCompanyId == ownerId);

        InvitationStatus[] joinedStatuses = SupplierAccessService.JoinedStatuses;

        var projected = query.Select(a => new
        {
            a.Id,
            a.Title,
            a.ReferenceCode,
            a.AuctionType,
            a.Status,
            a.OwnerCompanyId,
            OwnerName = a.OwnerCompany.Name,
            OwnerLogoUrl = a.OwnerCompany.LogoUrl,
            a.StartAt,
            a.EndAt,
            a.CreatedAt,
            ItemCount = a.Items.Count,
            ParticipantCount = a.Invitations.Count(i => joinedStatuses.Contains(i.Status)),
        });

        if (request.MinParticipants is int min)
            projected = projected.Where(x => x.ParticipantCount >= min);
        if (request.MaxParticipants is int max)
            projected = projected.Where(x => x.ParticipantCount <= max);

        projected = request.Sort switch
        {
            TenderSorts.Newest => projected.OrderByDescending(x => x.CreatedAt),
            TenderSorts.ParticipantsDesc => projected.OrderByDescending(x => x.ParticipantCount).ThenBy(x => x.EndAt),
            TenderSorts.ParticipantsAsc => projected.OrderBy(x => x.ParticipantCount).ThenBy(x => x.EndAt),
            _ => projected.OrderBy(x => x.EndAt),
        };

        var rows = await projected.Take(take).ToListAsync(ct);

        return rows.Select(r => new TenderSearchItemDto(
                r.Id, r.Title, r.ReferenceCode, r.AuctionType.ToString(), r.Status.ToString(),
                r.OwnerCompanyId, r.OwnerName, r.OwnerLogoUrl, r.StartAt, r.EndAt, r.ItemCount, r.ParticipantCount,
                access?.IsJoined(r.Id) ?? false))
            .ToList();
    }

    private async Task<IReadOnlyList<ProductSearchItemDto>> SearchProductsAsync(
        string? term, int take, SupplierAccessContext? access, IQueryable<Auction> openTenders, CancellationToken ct)
    {
        IQueryable<Product> query = _context.Products.Where(p => p.IsActive);

        // Tedarikçi iç ürün kataloğunun tamamını görmez: yalnızca görebildiği açık ihalelerde istenen ürünler.
        if (access is not null)
        {
            IQueryable<Guid> requestedProductIds = openTenders
                .SelectMany(a => a.Items)
                .Where(i => i.ProductId != null)
                .Select(i => i.ProductId!.Value);

            query = query.Where(p => requestedProductIds.Contains(p.Id));
        }

        if (term is not null)
        {
            query = query.Where(p =>
                p.Name.ToLower().Replace("ı", "i").Contains(term)
                || p.Sku.ToLower().Contains(term)
                || (p.Brand != null && p.Brand.ToLower().Replace("ı", "i").Contains(term))
                || p.Category.Name.ToLower().Replace("ı", "i").Contains(term));
        }

        var rows = await query
            .OrderBy(p => p.Name)
            .Take(take)
            .Select(p => new
            {
                p.Id,
                p.Sku,
                p.Name,
                CategoryName = p.Category.Name,
                p.UnitOfMeasure,
                ThumbnailUrl = p.Images.OrderByDescending(i => i.IsPrimary).ThenBy(i => i.SortOrder)
                    .Select(i => i.Url).FirstOrDefault(),
            })
            .ToListAsync(ct);

        List<Guid> ids = rows.Select(r => r.Id).ToList();
        Dictionary<Guid, int> tenderCounts = (await openTenders
                .SelectMany(a => a.Items)
                .Where(i => i.ProductId != null && ids.Contains(i.ProductId.Value))
                .Select(i => new { ProductId = i.ProductId!.Value, i.AuctionId })
                .Distinct()
                .ToListAsync(ct))
            .GroupBy(x => x.ProductId)
            .ToDictionary(g => g.Key, g => g.Count());

        return rows.Select(r => new ProductSearchItemDto(
                r.Id, r.Sku, r.Name, r.CategoryName, r.UnitOfMeasure.ToString(), r.ThumbnailUrl,
                tenderCounts.GetValueOrDefault(r.Id)))
            .ToList();
    }

    private static string? Normalize(string? term)
    {
        if (string.IsNullOrWhiteSpace(term)) return null;

        string t = term.Trim().ToLowerInvariant().Replace("i̇", "i").Replace("ı", "i");
        return t.Length > 100 ? t[..100] : t;
    }
}
