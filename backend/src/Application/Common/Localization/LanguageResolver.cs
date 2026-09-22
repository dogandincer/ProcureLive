namespace ProcureLive.Application.Common.Localization;

/// <summary>
/// Resolves the UI language ("tr" or "en") from a raw Accept-Language header value. The frontend
/// sends a bare code ("tr"/"en"), but this also tolerates the full RFC 7231 form
/// (e.g. "en-US,en;q=0.9,tr;q=0.8") so any standards-compliant client works too.
/// </summary>
public static class LanguageResolver
{
    public const string Default = "tr";

    public static string Resolve(string? acceptLanguageHeader)
    {
        if (string.IsNullOrWhiteSpace(acceptLanguageHeader))
        {
            return Default;
        }

        string firstEntry = acceptLanguageHeader.Split(',')[0].Split(';')[0].Trim();

        return firstEntry.StartsWith("en", StringComparison.OrdinalIgnoreCase) ? "en" : Default;
    }
}
