namespace ProcureLive.Contracts.Erp;

/// <summary>Premium özellik durumu: ön yüz ERP menüsünü/düğmelerini buna göre gösterir.</summary>
public sealed record FeaturesDto(bool ErpIntegration, string ErpMode);

/// <summary>Kayıtlı ERP ayarı. Şifre dönmez; yalnızca kayıtlı olup olmadığı.</summary>
public sealed record ErpSettingsDto(
    string Mode,
    string GetServiceUrl,
    string SaveServiceUrl,
    string Username,
    bool HasPassword,
    string CoCode,
    string BranchCode,
    string OfferDocTraCode,
    string OrderDocTraCode,
    string DocNoPrefix,
    string? EntityCodePrefix,
    bool ExportNewSuppliers,
    bool ExportNewProducts,
    DateTime? UpdatedAt,
    string? UpdatedBy);

/// <summary><see cref="Password"/> boşsa kayıtlı şifre korunur.</summary>
public sealed record ErpSettingsRequest(
    string Mode,
    string GetServiceUrl,
    string SaveServiceUrl,
    string Username,
    string? Password,
    string CoCode,
    string BranchCode,
    string OfferDocTraCode,
    string OrderDocTraCode,
    string DocNoPrefix,
    string? EntityCodePrefix,
    bool ExportNewSuppliers,
    bool ExportNewProducts);

public sealed record ErpTestResultDto(bool Succeeded, string? Error, int? EntityCount, int? ItemCount);

/// <summary>ERP'deki bir cari; <see cref="LinkedCompanyId"/> doluysa ProcureLive'da eşleşmiş kaydı vardır.</summary>
public sealed record ErpEntityDto(int Id, string Code, string Name, string? TaxNo, string? Phone, string? Email, Guid? LinkedCompanyId, string? LinkedCompanyName);

public sealed record ErpItemDto(int Id, string Code, string Name, string? UnitCode, string? Brand, Guid? LinkedProductId, string? LinkedProductName);

public sealed record ErpImportRequest(IReadOnlyCollection<string> Codes, Guid? WorkspaceId, Guid? CategoryId);

public sealed record ErpImportResultDto(int Created, int Updated, IReadOnlyCollection<string> Errors);

public sealed record ErpSyncJobDto(
    Guid Id,
    string Kind,
    string Status,
    string Summary,
    string? Message,
    int AttemptCount,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    Guid? AuctionId,
    Guid? CompanyId,
    Guid? ProductId,
    Guid? PurchaseOrderId);

public sealed record ErpSyncJobDetailDto(ErpSyncJobDto Job, string? RequestXml, string? ResponseText);

public sealed record ErpSyncSummaryDto(int Pending, int Simulated, int Done, int Failed, int Skipped);

public sealed record ErpOfferLinkDto(Guid CompanyId, string CompanyName, string DocNo, int? ErpOfferMId, DateTime CreatedAt, DateTime? LastRevisedBidAt, string? LastStatus, string? LastMessage);

/// <summary>Cari/ürün/sipariş/ihale kartlarındaki ERP kutusu: eşleme kodu, son işler, ihale için tedarikçi teklifleri.</summary>
public sealed record ErpRecordStatusDto(
    bool Enabled,
    string Mode,
    string? ErpCode,
    IReadOnlyCollection<ErpSyncJobDto> Jobs,
    IReadOnlyCollection<ErpOfferLinkDto> Offers);
