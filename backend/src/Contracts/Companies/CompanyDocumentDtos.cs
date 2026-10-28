namespace ProcureLive.Contracts.Companies;

/// <summary><see cref="ExpiryState"/>: "Valid", "ExpiringSoon" (30 gün içinde), "Expired" veya süresiz belgede "NoExpiry".</summary>
public sealed record CompanyDocumentDto(
    Guid Id,
    string DocumentType,
    string Title,
    string FileUrl,
    string FileName,
    DateTime? ValidUntil,
    string ExpiryState,
    DateTime CreatedAt);

public sealed record AddCompanyDocumentRequest(string DocumentType, string Title, string FileUrl, string FileName, DateTime? ValidUntil);
