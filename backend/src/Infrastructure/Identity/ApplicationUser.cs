using Microsoft.AspNetCore.Identity;

namespace ProcureLive.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public Guid? CompanyId { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime? LastLoginAt { get; set; }

    /// <summary>Art arda 3'ten fazla hatalı şifre nedeniyle otomatik pasife alındığı an (Admin aktifleştirince temizlenir).</summary>
    public DateTime? LockedOutAt { get; set; }

    /// <summary>Kullanıcı bildirimlerini e-postayla da almak istiyor mu (Profil ekranından kendisi değiştirir).</summary>
    public bool EmailNotificationsEnabled { get; set; } = true;
}
