using System.Globalization;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using ProcureLive.Application.Common.Interfaces;

namespace ProcureLive.Infrastructure.Services;

/// <summary>
/// Fetches today's rates from the Turkish Central Bank (TCMB)'s public, unauthenticated XML feed.
/// TCMB publishes ForexBuying/ForexSelling/BanknoteBuying/BanknoteSelling per currency, quoted as
/// "price of Unit currency units in TRY" - ForexBuying is the standard reference rate used here,
/// divided by Unit to normalize to a per-1-unit rate (most currencies have Unit=1; a few low-value
/// currencies are quoted per 100).
/// </summary>
public sealed class TcmbExchangeRateProvider : IExchangeRateProvider
{
    private const string FeedUrl = "https://www.tcmb.gov.tr/kurlar/today.xml";

    private readonly HttpClient _httpClient;
    private readonly ILogger<TcmbExchangeRateProvider> _logger;

    public TcmbExchangeRateProvider(HttpClient httpClient, ILogger<TcmbExchangeRateProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public string SourceName => "TCMB";

    public async Task<IReadOnlyCollection<ExternalExchangeRate>> GetLatestRatesAsync(CancellationToken cancellationToken)
    {
        string xml = await _httpClient.GetStringAsync(FeedUrl, cancellationToken);
        XDocument document = XDocument.Parse(xml);

        var rates = new List<ExternalExchangeRate>();

        foreach (XElement currencyElement in document.Descendants("Currency"))
        {
            string? code = currencyElement.Attribute("CurrencyCode")?.Value ?? currencyElement.Attribute("Kod")?.Value;

            if (string.IsNullOrWhiteSpace(code))
            {
                continue;
            }

            string? forexBuyingText = currencyElement.Element("ForexBuying")?.Value;
            string? unitText = currencyElement.Element("Unit")?.Value;

            decimal forexBuying;
            bool parsedRate = decimal.TryParse(forexBuyingText, NumberStyles.Number, CultureInfo.InvariantCulture, out forexBuying);

            decimal unit;
            bool parsedUnit = decimal.TryParse(unitText, NumberStyles.Number, CultureInfo.InvariantCulture, out unit);

            if (!parsedRate || forexBuying <= 0)
            {
                _logger.LogWarning("TCMB: '{Code}' için kur değeri okunamadı, atlanıyor.", code);
                continue;
            }

            if (!parsedUnit || unit <= 0)
            {
                unit = 1;
            }

            rates.Add(new ExternalExchangeRate(code, forexBuying / unit));
        }

        return rates;
    }
}
