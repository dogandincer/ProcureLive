namespace ProcureLive.Application.Common.Privacy;

/// <summary>
/// KVKK: iletişim paylaşımına izin vermeyen firmaların verisi sunucu tarafında maskelenir
/// (yalnızca CSS ile gizlemek yeterli değildir — ham veri istemciye hiç gitmez).
/// </summary>
public static class ContactMasker
{
    /// <summary>"test@example.com" → "te**@***.com"</summary>
    public static string? MaskEmail(string? email)
    {
        if (string.IsNullOrWhiteSpace(email)) return email;

        int at = email.IndexOf('@');
        if (at <= 0) return "***";

        string local = email[..at];
        string domain = email[(at + 1)..];
        string maskedLocal = local.Length <= 2 ? local[..1] + "*" : local[..2] + "**";
        int dot = domain.LastIndexOf('.');
        string tld = dot >= 0 ? domain[dot..] : string.Empty;

        return $"{maskedLocal}@***{tld}";
    }

    /// <summary>"+90 532 123 4567" → "+90*******" (uzunluk da sızdırılmaz).</summary>
    public static string? MaskPhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone)) return phone;

        string digits = new(phone.Where(char.IsDigit).ToArray());
        if (phone.TrimStart().StartsWith('+') && digits.Length >= 2)
        {
            return "+" + digits[..2] + "*******";
        }

        return (digits.Length >= 1 ? digits[..1] : string.Empty) + "*******";
    }

    /// <summary>Kısa adres: "Beykoz/İstanbul" (ilçe yoksa yalnızca il).</summary>
    public static string? ShortAddress(string? district, string? city)
    {
        if (string.IsNullOrWhiteSpace(city)) return null;
        return string.IsNullOrWhiteSpace(district) ? city : $"{district}/{city}";
    }
}
