using MediatR;
using ProcureLive.Domain.Enums;
using ProcureLive.Application.Common.Security;

namespace ProcureLive.Application.Auctions.Commands.CreateTender;

/// <summary>
/// Tek transaction'da ihale (master) + kalemler + davetler + ekleri oluşturur.
/// Enum alanları controller'da parse edilip buraya tiplenmiş gelir.
/// </summary>
[RequiresPermission(PermissionCodes.AuctionsManage)]
public sealed record CreateTenderCommand(
    string? ReferenceCode,
    string Title,
    string? Description,
    AuctionType AuctionType,
    Guid OwnerCompanyId,
    Guid CurrencyId,
    DateTime StartAt,
    DateTime EndAt,
    decimal? ReservePrice,
    int ExtensionTriggerMinutes,
    int? ExtensionDurationMinutes,
    int? MaxExtensionMinutes,
    PaymentTerm? PaymentTerm,
    DeliveryTerm? DeliveryTerm,
    bool AllowAlternativeProducts,
    bool AllowPartialBids,
    IReadOnlyCollection<TenderLineInput> Lines,
    IReadOnlyCollection<Guid> VendorIds,
    IReadOnlyCollection<TenderAttachmentInput> Attachments,
    bool IsListedForPlus = false,
    Guid? WorkspaceId = null) : IRequest<Guid>, IOwnerFirmScopedRequest, IWorkspaceTargetRequest;

public sealed record TenderLineInput(
    Guid? ProductId,
    string Description,
    decimal Quantity,
    UnitOfMeasure UnitOfMeasure,
    decimal? TargetPrice,
    DateTime? RequestedDeliveryDate,
    string? Specification,
    int SortOrder);

public sealed record TenderAttachmentInput(
    string FileName,
    string Url,
    string? ContentType,
    long SizeBytes);
