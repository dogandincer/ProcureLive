namespace ProcureLive.Contracts.Rfi;

public sealed record RfiTemplateDto(
    Guid Id,
    string Name,
    string? Description,
    Guid? CategoryId,
    bool IsActive,
    IReadOnlyCollection<RfiQuestionDto> Questions);

public sealed record RfiQuestionDto(
    Guid Id,
    string QuestionText,
    string QuestionType,
    bool IsRequired,
    int SortOrder,
    decimal Weight,
    IReadOnlyCollection<RfiQuestionOptionDto> Options);

public sealed record RfiQuestionOptionDto(Guid Id, string OptionText, decimal ScoreValue);

public sealed record CreateRfiTemplateRequest(string Name, string? Description, Guid? CategoryId);

public sealed record AddRfiQuestionRequest(
    string QuestionText,
    string QuestionType,
    bool IsRequired,
    int SortOrder,
    decimal Weight,
    IReadOnlyCollection<string>? Options);

public sealed record RfiProcessDto(
    Guid Id,
    Guid RfiTemplateId,
    string TemplateName,
    string Title,
    Guid OwnerCompanyId,
    string OwnerCompanyName,
    string Status,
    DateTime DueDate,
    int InvitationCount,
    int ResponseCount,
    DateTime CreatedAt,
    string AccessLevel = "Full",
    string? MyInvitationStatus = null);

public sealed record RfiProcessDetailDto(
    Guid Id,
    Guid RfiTemplateId,
    string TemplateName,
    string Title,
    Guid OwnerCompanyId,
    string OwnerCompanyName,
    string Status,
    DateTime DueDate,
    IReadOnlyCollection<RfiQuestionDto> Questions,
    IReadOnlyCollection<RfiInvitationDto> Invitations,
    string AccessLevel = "Full",
    bool IsOpen = false,
    bool HasSubmitted = false,
    bool CanSubmit = false,
    IReadOnlyCollection<RfiMyAnswerDto>? MyAnswers = null);

/// <summary>Tedarikçinin kendi gönderdiği yanıt (revizyon formunu doldurmak için).</summary>
public sealed record RfiMyAnswerDto(
    Guid RfiQuestionId,
    string? AnswerText,
    decimal? AnswerNumber,
    bool? AnswerBool,
    Guid? SelectedOptionId);

public sealed record RfiInvitationDto(
    Guid Id,
    Guid CompanyId,
    string CompanyName,
    string Status,
    DateTime? SentAt,
    DateTime? RespondedAt);

public sealed record CreateRfiProcessRequest(Guid RfiTemplateId, string Title, Guid OwnerCompanyId, DateTime DueDate);

public sealed record InviteCompaniesToRfiRequest(IReadOnlyCollection<Guid> CompanyIds);

public sealed record RespondToRfiInvitationRequest(bool Accept, string? DeclineReason);

public sealed record RfiAnswerRequest(
    Guid RfiQuestionId,
    string? AnswerText,
    decimal? AnswerNumber,
    bool? AnswerBool,
    Guid? SelectedOptionId,
    string? AttachmentUrl);

public sealed record SubmitRfiResponsesRequest(IReadOnlyCollection<RfiAnswerRequest> Answers);

public sealed record RfiResponseDto(
    Guid RfiQuestionId,
    string QuestionText,
    Guid CompanyId,
    string CompanyName,
    string? AnswerText,
    decimal? AnswerNumber,
    bool? AnswerBool,
    string? SelectedOptionText,
    string? AttachmentUrl);

public sealed record EvaluateSupplierRequest(Guid CompanyId, decimal TotalScore, string? Comments);

public sealed record SupplierEvaluationDto(
    Guid Id,
    Guid CompanyId,
    string CompanyName,
    decimal TotalScore,
    string? Comments,
    DateTime EvaluatedAt);
