namespace ProcureLive.Domain.Enums;

/// <summary>SMTP bağlantı güvenliği. Auto: sunucunun desteklediğine göre STARTTLS/SSL seçilir.</summary>
public enum EmailSecurityMode
{
    None = 0,
    StartTls = 1,
    SslOnConnect = 2,
    Auto = 3
}
