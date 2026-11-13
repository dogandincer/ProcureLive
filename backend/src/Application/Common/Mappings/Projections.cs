using System.Linq.Expressions;
using ProcureLive.Contracts.Catalog;
using ProcureLive.Contracts.Companies;
using ProcureLive.Contracts.Finance;
using ProcureLive.Domain.Entities.Catalog;
using ProcureLive.Domain.Entities.Companies;
using ProcureLive.Domain.Entities.Finance;

namespace ProcureLive.Application.Common.Mappings;

/// <summary>
/// Entity → DTO izdüşümleri. EF Core bunları doğrudan SQL'e çevirir (<c>query.Select(Projections.Product)</c>); yalnızca DTO'nun
/// ihtiyaç duyduğu kolonlar okunur. Haritalama kitaplığı kullanılmaz: alanlar derleme zamanında denetlenir.
/// </summary>
public static class Projections
{
    public static readonly Expression<Func<Currency, CurrencyDto>> Currency = c =>
        new CurrencyDto(c.Id, c.Code, c.Name, c.Symbol, c.IsBaseCurrency);

    public static readonly Expression<Func<ExchangeRate, ExchangeRateDto>> ExchangeRate = r =>
        new ExchangeRateDto(r.Id, r.CurrencyId, r.Currency.Code, r.RateDate, r.RateToBase, r.Source);

    public static readonly Expression<Func<Product, ProductDto>> Product = p =>
        new ProductDto(
            p.Id,
            p.Sku,
            p.Name,
            p.Description,
            p.CategoryId,
            p.Category.Name,
            p.UnitOfMeasure.ToString(),
            p.Brand,
            p.DefaultCurrencyId,
            p.IsActive,
            p.WorkspaceId,
            p.Workspace != null ? p.Workspace.Name : null,
            p.Model3dUrl,
            p.Model3dSource,
            p.Images.Select(i => new ProductImageDto(i.Id, i.Url, i.IsPrimary, i.SortOrder)).ToList());

    /// <summary>
    /// Liste satırı. Puan ortalaması ve değerlendirme sayısı burada boş gelir; sorgu, sayfadaki cariler için tek bir gruplu
    /// sorguyla doldurur (bkz. GetCompaniesListQueryHandler).
    /// </summary>
    public static readonly Expression<Func<Company, CompanyDto>> Company = c =>
        new CompanyDto(
            c.Id,
            c.Code,
            c.Name,
            c.TaxNumber,
            c.TaxOffice,
            c.CompanyType.ToString(),
            c.Status.ToString(),
            c.Website,
            c.WorkspaceId,
            c.Workspace != null ? c.Workspace.Name : null,
            null,
            0,
            c.CreatedAt,
            c.VisibleWorkspaces.Select(v => v.Workspace.Name).ToList());

    /// <summary>Detay. Puan ortalaması ve sayısı, dönen değerlendirmelerden sorguda hesaplanır (bkz. GetCompanyByIdQueryHandler).</summary>
    public static readonly Expression<Func<Company, CompanyDetailDto>> CompanyDetail = c =>
        new CompanyDetailDto(
            c.Id,
            c.Code,
            c.Name,
            c.TradeName,
            c.TaxNumber,
            c.TaxOffice,
            c.MersisNo,
            c.CompanyType.ToString(),
            c.Status.ToString(),
            c.Website,
            c.Phone,
            c.Email,
            c.FoundationYear,
            c.EmployeeCount,
            c.Description,
            c.WorkspaceId,
            c.Workspace != null ? c.Workspace.Name : null,
            null,
            0,
            c.CreatedAt,
            c.ContactPersons
                .Select(p => new ContactPersonDto(p.Id, p.FirstName, p.LastName, p.Title, p.Email, p.Phone, p.IsPrimary))
                .ToList(),
            c.Addresses
                .Select(a => new AddressDto(a.Id, a.AddressType.ToString(), a.Country, a.City, a.District, a.PostalCode, a.Line1, a.Line2))
                .ToList(),
            c.Sectors
                .Select(s => new SectorDto(s.Sector.Id, s.Sector.Name, s.Sector.Description, s.Sector.IsActive, 0))
                .ToList(),
            c.Evaluations
                .Select(e => new CompanyEvaluationDto(
                    e.Id, e.CompanyId, c.Name, e.QualityScore, e.DeliveryScore, e.CommunicationScore, e.PriceScore,
                    e.ComplianceScore, e.OverallScore, e.Comments, e.SourceType, e.EvaluatedAt))
                .ToList(),
            c.VisibleWorkspaces.Select(v => v.WorkspaceId).ToList());
}
