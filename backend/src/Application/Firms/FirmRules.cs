using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Contracts.Firms;
using ProcureLive.Domain.Entities.Companies;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Firms;

/// <summary>Firma = CompanyType.Internal cari. Ortak sabitler, marka adı ve DTO eşlemesi.</summary>
public static class FirmRules
{
    public const string BaseAppName = "Kuzey e-İhale Yönetimi";
    public const string DefaultSuffix = "e-İhale Yönetim Sistemi";

    public static IQueryable<Company> Firms(IApplicationDbContext context) =>
        context.Companies.Where(c => c.CompanyType == CompanyType.Internal);

    public static BrandingDto Branding(Company? defaultFirm)
    {
        string suffix = string.IsNullOrWhiteSpace(defaultFirm?.AppTitleSuffix) ? DefaultSuffix : defaultFirm!.AppTitleSuffix!.Trim();
        string? shortName = string.IsNullOrWhiteSpace(defaultFirm?.ShortName) ? null : defaultFirm!.ShortName!.Trim();
        string appName = shortName is null ? BaseAppName : $"{shortName} {suffix}";
        return new BrandingDto(appName, shortName, suffix, defaultFirm?.LogoUrl, shortName is not null);
    }

    public static async Task<List<FirmDto>> ToDtosAsync(IApplicationDbContext context, IQueryable<Company> query, CancellationToken ct)
    {
        var rows = await query
            .OrderByDescending(c => c.IsDefaultFirm).ThenBy(c => c.Code).ThenBy(c => c.Name)
            .Select(c => new
            {
                Firm = c,
                Billing = c.Addresses.Where(a => a.AddressType == AddressType.Billing).OrderBy(a => a.CreatedAt).FirstOrDefault(),
                WorkspaceCount = context.Workspaces.Count(w => w.FirmId == c.Id),
            })
            .ToListAsync(ct);

        return rows.Select(r => new FirmDto(
            r.Firm.Id, r.Firm.Code ?? string.Empty, r.Firm.Name, r.Firm.TradeName, r.Firm.ShortName,
            r.Firm.Phone, r.Firm.Phone2, r.Firm.MobilePhone, r.Firm.Fax, r.Firm.Email, r.Firm.Email2, r.Firm.Website,
            NullIfEmpty(r.Firm.TaxOffice), NullIfEmpty(r.Firm.TaxNumber),
            r.Billing?.Country, r.Billing?.City, r.Billing?.District, r.Billing?.PostalCode, r.Billing?.Line1,
            r.Firm.LogoUrl, r.Firm.IsDefaultFirm, r.Firm.AppTitleSuffix, r.WorkspaceCount)).ToList();
    }

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
