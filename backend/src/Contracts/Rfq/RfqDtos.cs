namespace ProcureLive.Contracts.Rfq;

public sealed record RfqProcessDto(
    Guid Id,
    string Title,
    Guid OwnerCompanyId,
    string OwnerCompanyName,
    Guid CurrencyId,
    string CurrencyCode,
    string Status,
    DateTime DueDate,
    int LineItemCount,
    int InvitationCount,
    int QuoteCount,
    DateTime CreatedAt,
    string AccessLevel = "Full",
    string? MyInvitationStatus = null,
    string? WorkspaceName = null);

public sealed record RfqProcessDetailDto(
    Guid Id,
    string Title,
    Guid OwnerCompanyId,
    string OwnerCompanyName,
    Guid CurrencyId,
    string CurrencyCode,
    string Status,
    DateTime DueDate,
    Guid? WinningQuoteId,
    IReadOnlyCollection<RfqLineItemDto> LineItems,
    IReadOnlyCollection<RfqInvitationDto> Invitations,
    string AccessLevel = "Full",
    bool IsOpen = false,
    bool HasSubmitted = false,
    bool CanSubmit = false,
    Guid? WorkspaceId = null,
    string? WorkspaceName = null);

public sealed record RfqLineItemDto(
    Guid Id,
    Guid? ProductId,
    string Description,
    decimal Quantity,
    string UnitOfMeasure,
    int SortOrder);

public sealed record RfqInvitationDto(
    Guid Id,
    Guid CompanyId,
    string CompanyName,
    string Status,
    DateTime? SentAt,
    DateTime? RespondedAt);

public sealed record RfqQuoteDto(
    Guid Id,
    Guid CompanyId,
    string CompanyName,
    string Status,
    DateTime? SubmittedAt,
    DateTime? ValidUntil,
    string? Notes,
    IReadOnlyCollection<RfqQuoteLineItemDto> LineItems);

public sealed record RfqQuoteLineItemDto(
    Guid RfqLineItemId,
    string LineItemDescription,
    decimal UnitPrice,
    Guid CurrencyId,
    string CurrencyCode,
    decimal NormalizedUnitPriceInBaseCurrency,
    int? LeadTimeDays,
    string? Notes);

public sealed record CreateRfqProcessRequest(string Title, Guid OwnerCompanyId, Guid CurrencyId, DateTime DueDate, Guid? WorkspaceId = null);

public sealed record AddRfqLineItemRequest(Guid? ProductId, string Description, decimal Quantity, string UnitOfMeasure, int SortOrder);

public sealed record InviteCompaniesToRfqRequest(IReadOnlyCollection<Guid> CompanyIds);

public sealed record AwardRfqProcessRequest(Guid WinningQuoteId, string? Notes);

public sealed record RespondToRfqInvitationRequest(bool Accept, string? DeclineReason);

public sealed record RfqQuoteLineItemInput(Guid RfqLineItemId, decimal UnitPrice, Guid CurrencyId, int? LeadTimeDays, string? Notes);

public sealed record SubmitRfqQuoteRequest(
    DateTime? ValidUntil,
    string? Notes,
    IReadOnlyCollection<RfqQuoteLineItemInput> LineItems);

/// <summary>Rule-based AI recommendation for an RFQ process: which quote to select and why.</summary>
public sealed record RfqAiRecommendationDto(
    Guid RecommendedQuoteId,
    string RecommendedCompanyName,
    decimal ConfidenceScore,
    string Rationale,
    IReadOnlyCollection<RfqQuoteScoreDto> Scores);

public sealed record RfqQuoteScoreDto(
    Guid QuoteId,
    string CompanyName,
    decimal TotalScore,
    decimal PriceScore,
    decimal LeadTimeScore,
    decimal TotalNormalizedCost,
    double? AverageLeadTimeDays);

/// <summary>Admin comparison matrix row: one supplier's quote for one line item, normalized for cross-currency comparison.</summary>
public sealed record RfqComparisonRowDto(
    Guid RfqLineItemId,
    string LineItemDescription,
    Guid CompanyId,
    string CompanyName,
    decimal UnitPrice,
    string CurrencyCode,
    decimal NormalizedUnitPriceInBaseCurrency,
    int? LeadTimeDays);
