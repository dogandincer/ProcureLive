using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Notifications.Email;

/// <summary>
/// Bildirimin ön yüzdeki hedef sayfası (e-postadaki "Kaydı aç" düğmesi için). Ön yüzdeki notificationLink()
/// (layouts/default.vue) ile aynı kurallar: iç kullanıcı yönetim ekranına, tedarikçi kendi ekranına gider.
/// </summary>
public static class NotificationLinks
{
    public static string? For(NotificationType type, string? relatedEntityType, Guid? relatedEntityId, bool internalUser)
    {
        if (relatedEntityId is not Guid id) return null;
        string qa = type == NotificationType.ProcessQuestion ? "#qa" : "";

        return relatedEntityType switch
        {
            "Auction" => internalUser
                ? $"/admin/auctions/{id}{qa}"
                : $"/supplier/auctions/{id}{(type == NotificationType.ParticipationRequest ? "#participation" : qa)}",
            "RfiProcess" => (internalUser ? $"/admin/rfi/{id}" : $"/supplier/rfi/{id}") + qa,
            "RfqProcess" => (internalUser ? $"/admin/rfq/{id}" : $"/supplier/rfq/{id}") + qa,
            "Company" when type == NotificationType.DocumentExpiry => internalUser ? $"/admin/companies/{id}#documents" : "/supplier/settings#documents",
            "Company" => internalUser ? $"/admin/companies/{id}" : "/supplier",
            "PurchaseOrder" => internalUser ? $"/admin/orders/{id}" : $"/supplier/orders/{id}",
            _ => null
        };
    }
}
