using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using ProcureLive.Application.Common.Interfaces;

namespace ProcureLive.Infrastructure.Services.Email;

/// <summary>
/// Tüm giden e-postaların ortak görünümü. Gövde düz metin olarak gelir (bildirim metni); burada HTML'e kaçışlanıp satır sonları
/// paragrafa, adresler bağlantıya çevrilir. E-posta istemcileri harici CSS'i desteklemediği için stiller satır içidir.
/// </summary>
public static partial class EmailTemplate
{
    private const string Accent = "#4f46e5";

    public static string RenderText(EmailMessage message)
    {
        var text = new StringBuilder(message.Body.Trim());
        if (!string.IsNullOrWhiteSpace(message.ActionUrl))
        {
            text.Append("\n\n").Append(message.ActionLabel ?? "Aç").Append(": ").Append(message.ActionUrl);
        }

        return text.ToString();
    }

    public static string RenderHtml(EmailMessage message, string brandName)
    {
        string brand = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(brandName) ? "e-İhale" : brandName);
        string title = WebUtility.HtmlEncode(message.Subject);

        string paragraphs = string.Join("", ParagraphSplit().Split(message.Body.Replace("\r\n", "\n").Trim())
            .Where(p => p.Length > 0)
            .Select(p => $"<p style=\"margin:0 0 14px;line-height:1.6;\">{Linkify(WebUtility.HtmlEncode(p)).Replace("\n", "<br>")}</p>"));

        string button = string.IsNullOrWhiteSpace(message.ActionUrl)
            ? ""
            : $"""
               <p style="margin:24px 0 8px;"><a href="{WebUtility.HtmlEncode(message.ActionUrl)}" style="display:inline-block;background:{Accent};color:#ffffff;text-decoration:none;font-weight:600;padding:10px 20px;border-radius:6px;">{WebUtility.HtmlEncode(message.ActionLabel ?? "Aç")}</a></p>
               <p style="margin:0;font-size:12px;color:#64748b;">Düğme çalışmazsa: <a href="{WebUtility.HtmlEncode(message.ActionUrl)}" style="color:{Accent};word-break:break-all;">{WebUtility.HtmlEncode(message.ActionUrl)}</a></p>
               """;

        return $"""
            <!doctype html>
            <html lang="tr"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>{title}</title></head>
            <body style="margin:0;padding:0;background:#f1f5f9;font-family:Segoe UI,Helvetica,Arial,sans-serif;color:#1e293b;">
              <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background:#f1f5f9;padding:24px 12px;">
                <tr><td align="center">
                  <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="max-width:560px;background:#ffffff;border-radius:10px;border:1px solid #e2e8f0;">
                    <tr><td style="padding:18px 24px;border-bottom:3px solid {Accent};font-size:15px;font-weight:700;color:#0f172a;">{brand}</td></tr>
                    <tr><td style="padding:24px;font-size:14px;">
                      <h1 style="margin:0 0 16px;font-size:18px;color:#0f172a;">{title}</h1>
                      {paragraphs}
                      {button}
                    </td></tr>
                    <tr><td style="padding:14px 24px;border-top:1px solid #e2e8f0;font-size:11px;color:#94a3b8;">Bu e-posta {brand} tarafından otomatik gönderilmiştir. Bildirim e-postalarını Profil ekranından kapatabilirsiniz.</td></tr>
                  </table>
                </td></tr>
              </table>
            </body></html>
            """;
    }

    private static string Linkify(string encoded) =>
        UrlPattern().Replace(encoded, m => $"<a href=\"{m.Value}\" style=\"color:{Accent};\">{m.Value}</a>");

    [GeneratedRegex(@"\n\s*\n")]
    private static partial Regex ParagraphSplit();

    [GeneratedRegex(@"https?://[^\s<]+")]
    private static partial Regex UrlPattern();
}
