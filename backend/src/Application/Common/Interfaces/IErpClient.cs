using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Common.Interfaces;

/// <summary>Sunucu yapılandırmasındaki premium özellik anahtarları (Features bölümü); ekrandan değiştirilemez.</summary>
public interface IFeatureFlags
{
    /// <summary>ERP (UyumERP) entegrasyonu lisanslı mı.</summary>
    bool ErpIntegration { get; }
}

/// <summary>Kayıtlı ERP ayarının çözülmüş hali (şifre açık); kısa süre önbelleklenir, kayıttan sonra <see cref="Invalidate"/>.</summary>
public interface IErpSettingsProvider
{
    Task<ErpConnection> GetAsync(CancellationToken cancellationToken);

    void Invalidate();
}

public sealed record ErpConnection(
    ErpMode Mode,
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
    bool ExportNewProducts)
{
    public static readonly ErpConnection Off = new(ErpMode.Off, "", "", "", null, "", "", "", "", "PL-", null, false, false);

    public bool CanRead => !string.IsNullOrWhiteSpace(GetServiceUrl) && !string.IsNullOrWhiteSpace(Username);
}

/// <summary>Sonuç: ERP'nin "başarılı" bayrağı ve mesajı; ham yanıt kayıt ekranı için saklanır.</summary>
public sealed record ErpCallResult(bool Success, string? Message, string RawResponse);

/// <summary>UyumERP SOAP servisleri (UyumGetWebService / UyumSaveWebService).</summary>
public interface IErpClient
{
    /// <summary>GetDataTableWithSQL: salt okuma sorgusu, satırlar kolon adı → değer.</summary>
    Task<IReadOnlyList<IReadOnlyDictionary<string, string?>>> QueryAsync(ErpConnection connection, string sql, CancellationToken cancellationToken);

    /// <summary>UyumSaveWebService işlemi. <paramref name="valueXml"/> isteğin Value gövdesidir; kimlik bilgisini istemci ekler.</summary>
    Task<ErpCallResult> SaveAsync(ErpConnection connection, ErpSaveOperation operation, string valueXml, CancellationToken cancellationToken);

    /// <summary>Kayıt ekranında gösterilecek tam SOAP zarfı (şifre maskeli).</summary>
    string DescribeEnvelope(ErpConnection connection, ErpSaveOperation operation, string valueXml);
}

public enum ErpSaveOperation
{
    SaveEntity,
    SaveItem,
    SaveOffer,
    SaveOrderM,
    SaveUyumObjectFromXML
}
