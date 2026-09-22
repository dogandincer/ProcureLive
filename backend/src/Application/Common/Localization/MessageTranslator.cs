using System.Text.RegularExpressions;

namespace ProcureLive.Application.Common.Localization;

/// <summary>
/// Translates the Turkish messages produced by validators, exceptions and notifications into English,
/// applied once at the API response boundary (<see cref="ProcureLive.WebApi.Middleware.ExceptionHandlingMiddleware"/>)
/// and when notifications are read back (GetMyNotificationsQueryHandler). Call sites keep writing plain
/// Turkish as before - nothing upstream needs to know about languages. Static, non-interpolated messages
/// are looked up verbatim; the handful of interpolated messages (bid direction, role lists, notification
/// bodies with an auction/company name baked in) are matched with a small set of template rules. Anything
/// not recognized is returned unchanged (Turkish) rather than throwing - a missing translation should never
/// break a response.
/// </summary>
public static class MessageTranslator
{
    public static string Translate(string? text, string language)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text ?? string.Empty;
        }

        if (!string.Equals(language, "en", StringComparison.OrdinalIgnoreCase))
        {
            return text;
        }

        if (StaticPhrases.TryGetValue(text, out string? exact))
        {
            return exact;
        }

        foreach ((Regex pattern, MatchEvaluator render) in TemplateRules)
        {
            Match match = pattern.Match(text);
            if (match.Success)
            {
                return render(match);
            }
        }

        return text;
    }

    private static readonly Dictionary<string, string> StaticPhrases = new()
    {
        // ExceptionHandlingMiddleware's own literals
        ["Doğrulama hatası."] = "Validation error.",
        ["Beklenmeyen bir sunucu hatası oluştu."] = "An unexpected server error occurred.",

        // Exception class defaults
        ["Bir veya daha fazla doğrulama hatası oluştu."] = "One or more validation errors occurred.",
        ["Bu kaynağa erişim yetkiniz yok."] = "You do not have permission to access this resource.",

        // FluentValidation WithMessage(...) literals
        ["Alıcı Cari bulunamadı."] = "Buyer company not found.",
        ["Bir kategori kendi üst kategorisi olamaz."] = "A category cannot be its own parent category.",
        ["Bitiş tarihi başlangıçtan sonra olmalıdır."] = "End date must be after the start date.",
        ["Bu SKU'ya sahip bir ürün zaten mevcut."] = "A product with this SKU already exists.",
        ["Bu koda sahip bir kategori zaten mevcut."] = "A category with this code already exists.",
        ["Bu koda sahip bir para birimi zaten mevcut."] = "A currency with this code already exists.",
        ["Bu para birimi ve tarih için zaten bir kur kaydı mevcut."] = "An exchange rate for this currency and date already exists.",
        ["Bu vergi numarasına sahip cari zaten mevcut."] = "A company with this tax number already exists.",
        ["Kategori bulunamadı."] = "Category not found.",
        ["Para birimi bulunamadı."] = "Currency not found.",
        ["RFI şablonu bulunamadı."] = "RFI template not found.",
        ["Reddetme gerekçesi zorunludur."] = "A decline reason is required.",
        ["Supplier rolündeki kullanıcılar bir Cari'ye bağlı olmalıdır."] = "Users with the Supplier role must be linked to a company.",
        ["Üst kategori bulunamadı."] = "Parent category not found.",

        // Exception constructor literals
        ["Alt kategorileri olan bir kategori silinemez."] = "A category with subcategories cannot be deleted.",
        ["Başlangıç tarihi gelecekte olmalıdır."] = "Start date must be in the future.",
        ["Bu RFI süreci zaten kapatılmış."] = "This RFI process is already closed.",
        ["Bu RFI sürecine yanıt vermek için davetin kabul edilmiş olması gerekir."] = "The invitation must be accepted to respond to this RFI process.",
        ["Bu RFQ süreci zaten kapatılmış."] = "This RFQ process is already closed.",
        ["Bu davete zaten yanıt verilmiş."] = "This invitation has already been responded to.",
        ["Bu değişiklik kategori ağacında bir döngü oluşturur."] = "This change would create a cycle in the category tree.",
        ["Bu ihaleye teklif vermek için davetin kabul edilmiş olması gerekir."] = "The invitation must be accepted to bid on this auction.",
        ["Bu kalem için zaten bir kazanan seçilmiş."] = "A winner has already been selected for this item.",
        ["E-posta veya şifre hatalı."] = "Incorrect email or password.",
        ["Geçersiz veya süresi dolmuş refresh token."] = "Invalid or expired refresh token.",
        ["Kullanıcı bulunamadı veya pasif."] = "User not found or inactive.",
        ["Kullanıcının bağlı olduğu bir Cari bulunamadı."] = "The company this user belongs to was not found.",
        ["Sadece taslak (Draft) durumundaki RFQ süreçlerine kalem eklenebilir."] = "Items can only be added to RFQ processes in Draft status.",
        ["Sadece taslak (Draft) durumundaki ihalelere kalem eklenebilir."] = "Items can only be added to auctions in Draft status.",
        ["Sona ermiş veya sonuçlanmış bir ihale iptal edilemez."] = "An auction that has ended or been awarded cannot be cancelled.",
        ["Teklif verebilmek için davetin kabul edilmiş olması gerekir."] = "The invitation must be accepted to place a bid.",
        ["Teklif verilen kalem bu RFQ sürecine ait değil."] = "The item quoted does not belong to this RFQ process.",
        ["Yalnızca Admin kullanıcılar Admin veya Buyer rolünde hesap oluşturabilir."] = "Only Admin users can create accounts with the Admin or Buyer role.",
        ["Yalnızca canlı (Live) durumdaki ihalelerin süresi uzatılabilir."] = "Only auctions in Live status can be extended.",
        ["Yalnızca kapatılmış RFQ süreçlerinde kazanan seçilebilir."] = "A winner can only be selected for closed RFQ processes.",
        ["Yalnızca sona ermiş (veya canlı) ihalelerde kazanan seçilebilir."] = "A winner can only be selected for auctions that have ended (or are live).",
        ["Yalnızca taslak (Draft) durumundaki ihaleler yayınlanabilir."] = "Only auctions in Draft status can be published.",
        ["Yalnızca taslak veya gönderilmiş RFI süreçlerine davet gönderilebilir."] = "Invitations can only be sent for RFI processes in Draft or Sent status.",
        ["Yalnızca taslak veya gönderilmiş RFQ süreçlerine davet gönderilebilir."] = "Invitations can only be sent for RFQ processes in Draft or Sent status.",
        ["Yalnızca taslak veya planlanmış ihalelere davet gönderilebilir."] = "Invitations can only be sent for auctions in Draft or Scheduled status.",
        ["Yanıtlanan soru bu RFI şablonuna ait değil."] = "The answered question does not belong to this RFI template.",
        ["Yayınlamadan önce en az bir Cari davet edilmiş olmalıdır."] = "At least one company must be invited before publishing.",
        ["Yayınlamadan önce en az bir kalem eklenmelidir."] = "At least one item must be added before publishing.",
        ["Yeni bitiş tarihi mevcut bitiş tarihinden sonra olmalıdır."] = "The new end date must be after the current end date.",
        ["İhale yayınlanmadan önce en az bir Cari davet edilmelidir."] = "At least one company must be invited before publishing the auction.",
        ["İhale yayınlanmadan önce en az bir kalem eklenmelidir."] = "At least one item must be added before publishing the auction.",
        ["İhale şu anda teklif kabul etmiyor."] = "The auction is not currently accepting bids.",
        ["Sistemde zaten bir baz para birimi tanımlı. Aynı anda yalnızca bir baz para birimi olabilir."] =
            "The system already has a base currency defined. Only one base currency can exist at a time.",
        ["Supplier rolü atamak için önce kullanıcıya bir Cari atanmalıdır (Kullanıcı bilgilerini güncelleyin)."] =
            "A company must be assigned to the user before granting the Supplier role (update the user's details).",

        // Notification titles (static - only the bodies interpolate)
        ["İhaleyi Kazandınız"] = "You Won the Auction",
        ["İhale Sonuçlandı"] = "Auction Awarded",
        ["Yeni İhale Daveti"] = "New Auction Invitation",
        ["Teklifiniz Geçildi"] = "Your Bid Was Outbid",
        ["Davet Yanıtlandı"] = "Invitation Responded",
        ["Yeni Bilgi Talebi (RFI) Daveti"] = "New RFI Invitation",
        ["Teklifiniz Kazandı"] = "Your Quote Won",
        ["RFQ Sonuçlandı"] = "RFQ Awarded",
        ["Yeni Hızlı Teklif Talebi (RFQ) Daveti"] = "New RFQ Invitation",
        ["İhale Yakında Başlıyor"] = "Auction Starting Soon",
    };

    private static readonly (Regex Pattern, MatchEvaluator Render)[] TemplateRules =
    [
        // NotFoundException("Entity", key) -> auto-generated message
        (new Regex(@"^Entity ""([^""]+)"" \(([^)]+)\) bulunamadı\.$"),
            m => $"Entity \"{m.Groups[1].Value}\" ({m.Groups[2].Value}) not found."),

        // PlaceBidCommandHandler
        (new Regex(@"^Teklifiniz mevcut en iyi tekliften daha (yüksek|düşük) olmalıdır\.$"),
            m => $"Your bid must be {(m.Groups[1].Value == "yüksek" ? "higher" : "lower")} than the current best bid."),

        // RegisterCommandValidator / AssignRolesCommandValidator
        (new Regex(@"^Rol şunlardan biri olmalıdır: (.+)$"),
            m => $"Role must be one of: {m.Groups[1].Value}"),

        // CurrencyNormalizer
        (new Regex(@"^'([^']+)' para birimi için güncel bir kur tanımlı değil\.$"),
            m => $"No current exchange rate is defined for currency '{m.Groups[1].Value}'."),

        // AwardAuctionItemCommandHandler - winner
        (new Regex(@"^""([^""]+)"" ihalesinde ""([^""]+)"" kalemi için teklifiniz kazandı\.$"),
            m => $"Your bid won for item \"{m.Groups[2].Value}\" in auction \"{m.Groups[1].Value}\"."),

        // AwardAuctionItemCommandHandler - losing bidders
        (new Regex(@"^""([^""]+)"" ihalesinde ""([^""]+)"" kalemi için başka bir tedarikçinin teklifi seçildi\.$"),
            m => $"Another supplier's bid was selected for item \"{m.Groups[2].Value}\" in auction \"{m.Groups[1].Value}\"."),

        // InviteCompaniesCommandHandler (auction)
        (new Regex(@"^""([^""]+)"" ihalesine davet edildiniz\.$"),
            m => $"You have been invited to auction \"{m.Groups[1].Value}\"."),

        // PlaceBidCommandHandler - outbid
        (new Regex(@"^""([^""]+)"" ihalesinde ""([^""]+)"" kalemi için teklifiniz geçildi\.$"),
            m => $"Your bid was outbid for item \"{m.Groups[2].Value}\" in auction \"{m.Groups[1].Value}\"."),

        // AuctionLifecycleHostedService - starting soon reminder
        (new Regex(@"^""([^""]+)"" ihalesi yaklaşık (\d+) dakika içinde başlıyor\.$"),
            m => $"Auction \"{m.Groups[1].Value}\" starts in about {m.Groups[2].Value} minutes."),

        // RespondToInvitationCommandHandler (auction) - "kabul etti"/"reddetti" [+ optional Gerekçe: ...]
        (new Regex(@"^""([^""]+)"" ""([^""]+)"" ihalesi davetini (kabul etti|reddetti)\.(?: Gerekçe: (.+))?$"),
            m => RenderInvitationResponse(m, "auction")),

        // InviteCompaniesToRfiCommandHandler
        (new Regex(@"^""([^""]+)"" bilgi talebi sürecine davet edildiniz\.$"),
            m => $"You have been invited to the RFI process \"{m.Groups[1].Value}\"."),

        // RespondToRfiInvitationCommandHandler
        (new Regex(@"^""([^""]+)"" ""([^""]+)"" RFI süreci davetini (kabul etti|reddetti)\.(?: Gerekçe: (.+))?$"),
            m => RenderInvitationResponse(m, "RFI process")),

        // AwardRfqProcessCommandHandler - winner
        (new Regex(@"^""([^""]+)"" RFQ sürecinde teklifiniz kazandı\.$"),
            m => $"Your quote won in RFQ process \"{m.Groups[1].Value}\"."),

        // AwardRfqProcessCommandHandler - losing bidders
        (new Regex(@"^""([^""]+)"" RFQ sürecinde başka bir tedarikçinin teklifi seçildi\.$"),
            m => $"Another supplier's quote was selected in RFQ process \"{m.Groups[1].Value}\"."),

        // InviteCompaniesToRfqCommandHandler
        (new Regex(@"^""([^""]+)"" teklif toplama sürecine davet edildiniz\.$"),
            m => $"You have been invited to the RFQ process \"{m.Groups[1].Value}\"."),

        // RespondToRfqInvitationCommandHandler
        (new Regex(@"^""([^""]+)"" ""([^""]+)"" RFQ süreci davetini (kabul etti|reddetti)\.(?: Gerekçe: (.+))?$"),
            m => RenderInvitationResponse(m, "RFQ process")),
    ];

    private static string RenderInvitationResponse(Match m, string processNoun)
    {
        string company = m.Groups[1].Value;
        string title = m.Groups[2].Value;
        string action = m.Groups[3].Value == "kabul etti" ? "accepted" : "declined";
        string result = $"\"{company}\" {action} the invitation to {processNoun} \"{title}\".";

        return m.Groups[4].Success ? $"{result} Reason: {m.Groups[4].Value}" : result;
    }
}
