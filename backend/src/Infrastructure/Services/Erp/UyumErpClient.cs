using System.Net.Http.Headers;
using System.Security;
using System.Text;
using System.Xml.Linq;
using ProcureLive.Application.Common.Interfaces;

namespace ProcureLive.Infrastructure.Services.Erp;

/// <summary>
/// UyumERP SOAP istemcisi (OfferMaster'daki çağrılarla aynı zarflar). Okuma UyumGetWebService (SOAP 1.1, GetDataTableWithSQL),
/// yazma UyumSaveWebService (SOAP 1.2). SaveOffer'ın parametre adı "param", diğerlerinin "Context" (WSDL'e göre).
/// </summary>
public sealed class UyumErpClient : IErpClient
{
    private const string Tem = "http://tempuri.org/";
    private const string Soap11 = "http://schemas.xmlsoap.org/soap/envelope/";
    private const string Soap12 = "http://www.w3.org/2003/05/soap-envelope";

    private readonly HttpClient _http;

    public UyumErpClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<IReadOnlyList<IReadOnlyDictionary<string, string?>>> QueryAsync(ErpConnection connection, string sql, CancellationToken cancellationToken)
    {
        string envelope = $"""
            <?xml version="1.0" encoding="utf-8"?>
            <soapenv:Envelope xmlns:soapenv="{Soap11}" xmlns:tem="{Tem}">
              <soapenv:Header/>
              <soapenv:Body>
                <tem:GetDataTableWithSQL>
                  <tem:Context>
                    {Token(connection, mask: false)}
                    <tem:Value>{SecurityElement.Escape(sql)}</tem:Value>
                    <tem:Top>0</tem:Top><tem:PageIndex>0</tem:PageIndex><tem:PageSize>0</tem:PageSize>
                  </tem:Context>
                </tem:GetDataTableWithSQL>
              </soapenv:Body>
            </soapenv:Envelope>
            """;

        string response = await PostAsync(connection.GetServiceUrl, Tem + "GetDataTableWithSQL", envelope, "text/xml", cancellationToken);
        XDocument doc = XDocument.Parse(response);

        // Hata: Result=false + Message (tablo yok, SQL hatası, yetki).
        XElement? result = doc.Descendants(XName.Get("GetDataTableWithSQLResult", Tem)).FirstOrDefault();
        string? ok = result?.Elements().FirstOrDefault(e => e.Name.LocalName is "Result" or "Success")?.Value;
        if (ok == "false")
            throw new InvalidOperationException(result!.Elements().FirstOrDefault(e => e.Name.LocalName == "Message")?.Value ?? "ERP sorgusu başarısız.");

        // Satırlar diffgram içinde DocumentElement/Table1 (ad alanı yok).
        return doc.Descendants().Where(e => e.Name.LocalName == "Table1" && e.Parent?.Name.LocalName == "DocumentElement")
            .Select(row => (IReadOnlyDictionary<string, string?>)row.Elements()
                .GroupBy(c => c.Name.LocalName).ToDictionary(g => g.Key, g => (string?)g.First().Value))
            .ToList();
    }

    public async Task<ErpCallResult> SaveAsync(ErpConnection connection, ErpSaveOperation operation, string valueXml, CancellationToken cancellationToken)
    {
        string envelope = Envelope(connection, operation, valueXml, mask: false);
        string contentType = $"application/soap+xml; charset=utf-8; action=\"{Tem}{operation}\"";
        string response = await PostAsync(connection.SaveServiceUrl, Tem + operation, envelope, contentType, cancellationToken);

        XDocument doc = XDocument.Parse(response);
        XElement? fault = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "Fault");
        if (fault is not null)
            return new ErpCallResult(false, fault.Descendants().FirstOrDefault(e => e.Name.LocalName is "Text" or "faultstring")?.Value ?? "SOAP hatası", response);

        XElement? result = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == $"{operation}Result");
        string? flag = result?.Elements().FirstOrDefault(e => e.Name.LocalName is "Result" or "Success")?.Value;
        string? message = result?.Elements().FirstOrDefault(e => e.Name.LocalName == "Message")?.Value;
        return new ErpCallResult(string.Equals(flag, "true", StringComparison.OrdinalIgnoreCase), string.IsNullOrWhiteSpace(message) ? null : message, response);
    }

    public string DescribeEnvelope(ErpConnection connection, ErpSaveOperation operation, string valueXml) =>
        Envelope(connection, operation, valueXml, mask: true);

    private static string Envelope(ErpConnection c, ErpSaveOperation operation, string valueXml, bool mask)
    {
        string param = operation == ErpSaveOperation.SaveOffer ? "param" : "Context";
        string paging = operation == ErpSaveOperation.SaveUyumObjectFromXML ? "<tem:Top>0</tem:Top><tem:PageIndex>0</tem:PageIndex><tem:PageSize>0</tem:PageSize>" : "";
        return $"""
            <soap:Envelope xmlns:soap="{Soap12}" xmlns:tem="{Tem}">
              <soap:Header/>
              <soap:Body>
                <tem:{operation}>
                  <tem:{param}>
                    {Token(c, mask)}
                    <tem:Value>{valueXml}</tem:Value>
                    {paging}
                  </tem:{param}>
                </tem:{operation}>
              </soap:Body>
            </soap:Envelope>
            """;
    }

    private static string Token(ErpConnection c, bool mask) =>
        $"<tem:Token><tem:UserName>{SecurityElement.Escape(c.Username)}</tem:UserName>"
        + $"<tem:Password>{(mask ? "***" : SecurityElement.Escape(c.Password ?? ""))}</tem:Password></tem:Token>";

    private async Task<string> PostAsync(string url, string soapAction, string envelope, string contentType, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Content = new StringContent(envelope, Encoding.UTF8);
        request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType.StartsWith("text/xml") ? "text/xml; charset=utf-8" : contentType);
        if (contentType.StartsWith("text/xml")) request.Headers.Add("SOAPAction", soapAction);

        using HttpResponseMessage response = await _http.SendAsync(request, ct);
        string body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode && !body.Contains("Fault", StringComparison.Ordinal))
            throw new HttpRequestException($"ERP servisi {(int)response.StatusCode} döndü.");
        return body;
    }
}
