namespace ProcureLive.Contracts.Companies;

public sealed record CompanySettingsDto(
    Guid CompanyId,
    string CompanyName,
    string CompanyType,
    string? LogoUrl,
    bool ShowContactInfo,
    bool ListTendersForPlus,
    bool IsPlus,
    DateTime? PlusExpiresAt);

public sealed record UpdateCompanySettingsRequest(string? LogoUrl, bool ShowContactInfo, bool ListTendersForPlus);
