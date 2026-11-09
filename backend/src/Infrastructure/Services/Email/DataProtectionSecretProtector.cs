using System.Security.Cryptography;
using Microsoft.AspNetCore.DataProtection;
using ProcureLive.Application.Common.Interfaces;

namespace ProcureLive.Infrastructure.Services.Email;

/// <summary>
/// ASP.NET Core Data Protection ile sır şifreleme. Anahtarlar Windows'ta kullanıcı profilinde (DPAPI korumalı) durur;
/// sunucuya taşınırken DataProtection:KeysPath ile kalıcı bir klasör verilmeli, yoksa kayıtlı SMTP şifresi yeniden girilmelidir.
/// </summary>
public sealed class DataProtectionSecretProtector : ISecretProtector
{
    private readonly IDataProtector _protector;

    public DataProtectionSecretProtector(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("ProcureLive.EmailSettings.SmtpPassword.v1");
    }

    public string Protect(string plainText) => _protector.Protect(plainText);

    public string? Unprotect(string protectedText)
    {
        try
        {
            return _protector.Unprotect(protectedText);
        }
        catch (CryptographicException)
        {
            return null;
        }
    }
}
