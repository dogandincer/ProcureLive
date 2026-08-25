namespace ProcureLive.Application.Common.Security;

/// <summary>Custom JWT claim type names, shared between token issuance (Infrastructure) and authorization (Application).</summary>
public static class ProcureLiveClaimTypes
{
    public const string CompanyId = "companyId";
}

public static class Roles
{
    public const string Admin = "Admin";
    public const string Buyer = "Buyer";
    public const string Supplier = "Supplier";
}

public static class Policies
{
    public const string RequireAdmin = "RequireAdmin";
    public const string RequireBuyer = "RequireBuyer";
    public const string RequireSupplier = "RequireSupplier";
    public const string RequireInternalUser = "RequireInternalUser";
    public const string CompanyAccess = "CompanyAccess";
}
