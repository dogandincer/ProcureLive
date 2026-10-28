namespace ProcureLive.Contracts.Collaboration;

/// <summary>
/// Süreç soru-cevabı. Tedarikçiye başka firmanın sorusu yalnızca cevap herkese açıksa döner ve soran firma adı
/// (<see cref="CompanyName"/>) boş gelir; <see cref="IsMine"/> kendi sorusunu işaretler.
/// </summary>
public sealed record ProcessQuestionDto(
    Guid Id,
    string Question,
    string? Answer,
    bool IsPublic,
    bool IsAnnouncement,
    string? CompanyName,
    bool IsMine,
    DateTime CreatedAt,
    DateTime? AnsweredAt);

public sealed record AskQuestionRequest(string Question);

public sealed record AnswerQuestionRequest(string Answer, bool IsPublic);

public sealed record AnnouncementRequest(string Text);
