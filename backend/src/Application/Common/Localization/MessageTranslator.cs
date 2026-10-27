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
        ["İhaleye Katılım"] = "Auction Participation",
        ["Plus Abonelik"] = "Plus Subscription",

        // Davet linki / abonelik / arama / firma ayarları
        ["Davet linki geçersiz."] = "The invitation link is invalid.",
        ["Davet linkinin süresi dolmuş."] = "The invitation link has expired.",
        ["Bu ihale artık katılıma kapalı."] = "This auction is no longer open for participation.",
        ["Davet linki yalnızca tedarikçi carilere oluşturulabilir."] = "Invitation links can only be created for supplier companies.",
        ["Bu davet linkiyle zaten hesap oluşturulmuş. Lütfen giriş yapın."] =
            "An account has already been created with this invitation link. Please sign in.",
        ["Bu davet farklı bir e-posta adresi için oluşturulmuş."] = "This invitation was created for a different email address.",
        ["Bu e-posta ile zaten bir hesap var. Lütfen giriş yapın."] = "An account with this email already exists. Please sign in.",
        ["Bu davet başka bir cariye ait. Davet edilen firmanın hesabıyla giriş yapın."] =
            "This invitation belongs to another company. Sign in with an account of the invited company.",
        ["Geçerli bir e-posta adresi girin."] = "Enter a valid email address.",
        ["Şifre en az 8 karakter olmalıdır."] = "Password must be at least 8 characters.",
        ["Bu ihaleye zaten katıldınız."] = "You have already joined this auction.",
        ["Yalnızca planlanmış veya canlı ihalelere katılabilirsiniz."] = "You can only join scheduled or live auctions.",
        ["Kart üzerindeki isim zorunludur."] = "The cardholder name is required.",
        ["Kartın son 4 hanesi 4 rakamdan oluşmalıdır."] = "The card's last 4 digits must be 4 numbers.",
        ["Bu paket zaten aktif."] = "This plan is already active.",
        ["Detaylı arama yalnızca Plus üyelere açıktır."] = "Advanced search is available to Plus members only.",
        ["Logo adresi en fazla 1000 karakter olabilir."] = "The logo URL can be at most 1000 characters.",
        ["Geçerli bir logo adresi girin (http/https)."] = "Enter a valid logo URL (http/https).",

        // İhale durum yönetimi (AuctionStatusService / ChangeAuctionStatus)
        ["Bu durum değişikliği için gerekçe zorunludur."] = "A reason is required for this status change.",
        ["Gerekçe en fazla 500 karakter olabilir."] = "The reason can be at most 500 characters.",
        ["Bitiş zamanı en az 1 dakika ileride olmalıdır."] = "The end time must be at least 1 minute in the future.",
        ["İhaleyi yeniden açmak için yeni bir bitiş zamanı girin."] = "Enter a new end time to reopen the auction.",
        ["Kazanan seçilmiş kalemleri olan ihale yeniden açılamaz."] = "An auction with awarded items cannot be reopened.",
        ["Kazanan seçilmiş kalemleri olan ihale iptal edilemez."] = "An auction with awarded items cannot be cancelled.",
        ["İhale Başladı"] = "Auction Started",
        ["İhale Devam Ediyor"] = "Auction Resumed",
        ["İhale Yeniden Açıldı"] = "Auction Reopened",
        ["İhale Duraklatıldı"] = "Auction Paused",
        ["İhale Sona Erdi"] = "Auction Ended",
        ["İhale İptal Edildi"] = "Auction Cancelled",
        ["İhale Yayından Kaldırıldı"] = "Auction Unpublished",

        // Firma / işyeri / yetki
        ["Bu kayıt yetkili olduğunuz firma/işyerlerine ait değil."] = "This record does not belong to a firm/workspace you are authorized for.",
        ["Hesabınız art arda 3'ten fazla hatalı şifre girildiği için pasife alındı. Yöneticinize başvurun."] =
            "Your account was deactivated after more than 3 consecutive wrong passwords. Please contact your administrator.",
        ["Firma/işyeri yetkisi yalnızca Satınalmacı kullanıcılara verilir."] = "Firm/workspace access can only be granted to Buyer users.",
        ["Seçilen işyerlerinden biri bulunamadı."] = "One of the selected workspaces was not found.",
        ["Firma kodu zorunludur."] = "Firm code is required.",
        ["Firma adı zorunludur."] = "Firm name is required.",
        ["Bu kodla bir kayıt zaten mevcut."] = "A record with this code already exists.",
        ["Vergi / TC kimlik numarası 10 veya 11 haneli olmalıdır."] = "The tax / ID number must be 10 or 11 digits.",
        ["Varsayılan firma kaldırılamaz; önce başka bir firmayı varsayılan yapın."] = "The default firm cannot be unset; make another firm the default first.",
        ["En az bir firma tanımlı olmalıdır."] = "At least one firm must be defined.",
        ["Varsayılan firma silinemez; önce başka bir firmayı varsayılan yapın."] = "The default firm cannot be deleted; make another firm the default first.",
        ["İşyeri bağlı olan firma silinemez. Önce işyerlerini başka bir firmaya taşıyın."] = "A firm with workspaces cannot be deleted. Move its workspaces to another firm first.",
        ["İhale, RFI veya RFQ sahibi olan firma silinemez."] = "A firm that owns auctions, RFIs or RFQs cannot be deleted.",
        ["Logo dosyası en fazla 2 MB olabilir."] = "The logo file can be at most 2 MB.",
        ["Logo PNG, JPG, WEBP veya ICO formatında olmalıdır."] = "The logo must be a PNG, JPG, WEBP or ICO file.",
        ["Bağlı olduğu firma seçilmelidir."] = "A parent firm must be selected.",
        ["Seçilen firma bulunamadı."] = "The selected firm was not found.",
        ["Bu firmada aynı isimde bir işyeri zaten mevcut."] = "A workspace with this name already exists in this firm.",
        ["En az bir işyeri tanımlı olmalıdır."] = "At least one workspace must be defined.",
        ["Varsayılan işyeri silinemez; önce firmanın başka bir işyerini varsayılan yapın."] = "The default workspace cannot be deleted; make another workspace of the firm the default first.",
        ["Varsayılan işyeri kaldırılamaz; önce firmanın başka bir işyerini varsayılan yapın."] = "The default workspace cannot be unset; make another workspace of the firm the default first.",

        // Cari / kendi kaydı / katılım talebi
        ["Firma tanımı Firma Tanım ekranından yapılır; Cari ekranı yalnızca tedarikçiler içindir."] = "Firms are defined on the Firm Definition screen; the Company screen is for suppliers only.",
        ["Geçerli bir kullanıcı e-postası girin."] = "Enter a valid user e-mail.",
        ["Kullanıcı şifresi en az 8 karakter olmalıdır."] = "The user password must be at least 8 characters.",
        ["Kullanıcı adı zorunludur."] = "User first name is required.",
        ["Kullanıcı soyadı zorunludur."] = "User last name is required.",
        ["Bu e-posta ile zaten bir kullanıcı var."] = "A user with this e-mail already exists.",
        ["Yetkili olmadığınız bir işyeri seçtiniz."] = "You selected a workspace you are not authorized for.",
        ["Firma kullanıcıları Kullanıcı Tanım ekranından açılır."] = "Firm users are created on the User Definition screen.",
        ["Firmalar için işyeri görünürlüğü tanımlanmaz."] = "Workspace visibility is not defined for firms.",
        ["Vergi dairesi zorunludur."] = "Tax office is required.",
        ["VKN 10, TCKN 11 haneli olmalıdır."] = "Tax number must be 10 digits, national ID 11 digits.",
        ["Devam etmek için KVKK aydınlatma metnini onaylamalısınız."] = "You must accept the privacy (KVKK) notice to continue.",
        ["Bu vergi numarasıyla kayıtlı bir firma var. Firmanızın yöneticisinden kullanıcı ya da davet linki isteyin."] = "A company with this tax number is already registered. Ask your company administrator for a user account or an invite link.",
        ["Bu ihale için katılım talebiniz değerlendiriliyor."] = "Your participation request for this auction is under review.",
        ["Bu ihaleye katılmak için ihale sahibi firmanın cari onayı gerekiyor. Katılım talebi ve ön teklif gönderin."] = "Joining this auction requires supplier approval from the owner firm. Send a participation request with a preliminary offer.",
        ["En az bir kalem için ön teklif girin."] = "Enter a preliminary offer for at least one item.",
        ["Ön teklif birim fiyatı sıfırdan büyük olmalıdır."] = "The preliminary unit price must be greater than zero.",
        ["Aynı kalem birden fazla kez girilemez."] = "The same item cannot be entered more than once.",
        ["Yalnızca planlanmış veya canlı ihalelere katılım talebi gönderebilirsiniz."] = "You can only send participation requests to scheduled or live auctions.",
        ["Bu ihale için değerlendirilmekte olan bir katılım talebiniz var."] = "You already have a participation request under review for this auction.",
        ["Bu ihaleye onay gerekmeden doğrudan katılabilirsiniz."] = "You can join this auction directly without approval.",
        ["Ön teklifte bu ihaleye ait olmayan bir kalem var."] = "The preliminary offer contains an item that does not belong to this auction.",
        ["Bu ihalede kısmi teklif kapalı; tüm kalemler için ön teklif vermelisiniz."] = "Partial bids are disabled for this auction; you must offer on all items.",
        ["Cari onayı yalnızca bekleyen talepler için verilebilir."] = "Supplier approval can only be given to pending requests.",
        ["Pasif durumdaki cari onaylanamaz; önce Cari kartından aktifleştirin."] = "A passive supplier cannot be approved; activate it on the company card first.",
        ["Ön teklifi kabul etmeden önce cari onayı verilmelidir."] = "The supplier must be approved before accepting the preliminary offer.",
        ["Bu talep zaten sonuçlandırılmış."] = "This request has already been concluded.",
        ["İhale artık açık değil; tedarikçi dahil edilemez."] = "The auction is no longer open; the supplier cannot be added.",
        ["Tedarikçinin Standart üyelik katılım kotası dolu; Plus'a geçmeden ihaleye dahil edilemez."] = "The supplier's Standard membership quota is full; it cannot be added before upgrading to Plus.",
        ["Seçilen işyeri ihale sahibi firmaya ait değil."] = "The selected workspace does not belong to the auction's owner firm.",
        ["Katılım Talebiniz İletildi"] = "Participation Request Sent",
        ["Yeni Katılım Talebi"] = "New Participation Request",
        ["Cari Kaydınız Onaylandı"] = "Your Supplier Record Was Approved",
        ["Cari Onaylandı"] = "Supplier Approved",
        ["İhaleye Dahil Edildiniz"] = "You Were Added to the Auction",
        ["Katılımcı Eklendi"] = "Participant Added",
        ["Katılım Talebiniz Reddedildi"] = "Participation Request Rejected",
        ["Katılım Talebi Reddedildi"] = "Participation Request Rejected",
        ["Yeni Tedarikçi Kaydı"] = "New Supplier Registration",
    };

    private static readonly Dictionary<string, string> StatusNames = new()
    {
        ["Taslak"] = "Draft",
        ["Planlandı"] = "Scheduled",
        ["Canlı"] = "Live",
        ["Duraklatıldı"] = "Paused",
        ["Sona Erdi"] = "Ended",
        ["İptal Edildi"] = "Cancelled",
        ["Sonuçlandı"] = "Awarded",
    };

    private static string StatusName(string turkish) => StatusNames.TryGetValue(turkish, out string? en) ? en : turkish;

    private static string WithReason(string text, Group reason) => reason.Success ? $"{text} Reason: {reason.Value}" : text;

    private static readonly (Regex Pattern, MatchEvaluator Render)[] TemplateRules =
    [
        // NotFoundException("Entity", key) -> auto-generated message
        (new Regex(@"^Entity ""([^""]+)"" \(([^)]+)\) bulunamadı\.$"),
            m => $"Entity \"{m.Groups[1].Value}\" ({m.Groups[2].Value}) not found."),

        // JoinAuction / RespondToInvitation - Standart üyelik kotası
        (new Regex(@"^Standart üyelikte en fazla (\d+) ihaleye katılabilirsiniz\. Daha fazlası için Plus'a geçin\.$"),
            m => $"Standard members can join at most {m.Groups[1].Value} auctions. Upgrade to Plus for more."),

        // PurchaseSubscriptionCommandHandler - paket kullanıcı sayısını karşılamıyor
        (new Regex(@"^Firmanızda (\d+) kullanıcı var; bu paket en fazla (\d+) kullanıcı içindir\. Daha geniş bir paket seçin\.$"),
            m => $"Your company has {m.Groups[1].Value} users; this plan covers up to {m.Groups[2].Value} users. Choose a larger plan."),

        // JoinAuctionCommandHandler - iç kullanıcılara bildirim
        (new Regex(@"^""([^""]+)"" ""([^""]+)"" ihalesine katıldı\.$"),
            m => $"\"{m.Groups[1].Value}\" joined the auction \"{m.Groups[2].Value}\"."),

        // PurchaseSubscriptionCommandHandler - iç kullanıcılara bildirim
        (new Regex(@"^""([^""]+)"" Plus üyeliğe geçti\.$"),
            m => $"\"{m.Groups[1].Value}\" upgraded to Plus."),

        // AuctionStatusService - izin verilmeyen geçiş
        (new Regex(@"^""([^""]+)"" durumundan ""([^""]+)"" durumuna geçiş yapılamaz\.$"),
            m => $"The status cannot be changed from \"{StatusName(m.Groups[1].Value)}\" to \"{StatusName(m.Groups[2].Value)}\"."),

        // AuctionStatusService - tedarikçi bildirimleri
        (new Regex(@"^""([^""]+)"" ihalesi başladı\.$"),
            m => $"Auction \"{m.Groups[1].Value}\" has started."),
        (new Regex(@"^""([^""]+)"" ihalesi kaldığı yerden devam ediyor\.$"),
            m => $"Auction \"{m.Groups[1].Value}\" has resumed."),
        (new Regex(@"^""([^""]+)"" ihalesi yeniden teklife açıldı\.$"),
            m => $"Auction \"{m.Groups[1].Value}\" has been reopened for bidding."),
        (new Regex(@"^""([^""]+)"" ihalesi geçici olarak durduruldu\.(?: Gerekçe: (.+))?$"),
            m => WithReason($"Auction \"{m.Groups[1].Value}\" has been paused.", m.Groups[2])),
        (new Regex(@"^""([^""]+)"" ihalesi sona erdi\.$"),
            m => $"Auction \"{m.Groups[1].Value}\" has ended."),
        (new Regex(@"^""([^""]+)"" ihalesi iptal edildi\.(?: Gerekçe: (.+))?$"),
            m => WithReason($"Auction \"{m.Groups[1].Value}\" has been cancelled.", m.Groups[2])),
        (new Regex(@"^""([^""]+)"" ihalesi yayından kaldırıldı\.$"),
            m => $"Auction \"{m.Groups[1].Value}\" has been unpublished."),

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

        // Cari kodu / kullanıcı
        (new Regex(@"^""([^""]+)"" cari kodu başka bir kayıtta kullanılıyor\.$"),
            m => $"The company code \"{m.Groups[1].Value}\" is already used by another record."),
        (new Regex(@"^Cari oluşturuldu ancak kullanıcı açılamadı: (.+)$"),
            m => $"The company was created but its user could not be created: {m.Groups[1].Value}"),

        // Katılım talebi bildirimleri
        (new Regex(@"^""([^""]+)"" ihalesi için katılım talebiniz ve ön teklifiniz sorumlu satınalmacıya iletildi\.$"),
            m => $"Your participation request and preliminary offer for auction \"{m.Groups[1].Value}\" were sent to the responsible buyer."),
        (new Regex(@"^""([^""]+)"" ""([^""]+)"" ihalesine katılmak için cari onayı ve ön teklif gönderdi\.$"),
            m => $"\"{m.Groups[1].Value}\" requested supplier approval and sent a preliminary offer to join auction \"{m.Groups[2].Value}\"."),
        (new Regex(@"^""([^""]+)"" cari kaydınızı onayladı\. ""([^""]+)"" için ön teklifiniz değerlendiriliyor\.$"),
            m => $"\"{m.Groups[1].Value}\" approved your supplier record. Your preliminary offer for \"{m.Groups[2].Value}\" is under review."),
        (new Regex(@"^""([^""]+)"" cari olarak onaylandı; ""([^""]+)"" için ön teklifi kabul bekliyor\.$"),
            m => $"\"{m.Groups[1].Value}\" was approved as a supplier; its preliminary offer for \"{m.Groups[2].Value}\" awaits acceptance."),
        (new Regex(@"^Ön teklifiniz kabul edildi; ""([^""]+)"" ihalesine katılımcı olarak dahil edildiniz\.$"),
            m => $"Your preliminary offer was accepted; you were added to auction \"{m.Groups[1].Value}\" as a participant."),
        (new Regex(@"^""([^""]+)"" ön teklifi kabul edilerek ""([^""]+)"" ihalesine dahil edildi\.$"),
            m => $"\"{m.Groups[1].Value}\" was added to auction \"{m.Groups[2].Value}\" after its preliminary offer was accepted."),
        (new Regex(@"^""([^""]+)"" ihalesi için katılım talebiniz reddedildi\.(?: Gerekçe: (.+))?$"),
            m => WithReason($"Your participation request for auction \"{m.Groups[1].Value}\" was rejected.", m.Groups[2])),
        (new Regex(@"^""([^""]+)"" firmasının ""([^""]+)"" katılım talebi reddedildi\.(?: Gerekçe: (.+))?$"),
            m => WithReason($"The participation request of \"{m.Groups[1].Value}\" for \"{m.Groups[2].Value}\" was rejected.", m.Groups[3])),
        (new Regex(@"^""([^""]+)"" kendi kaydını oluşturdu \(onay bekliyor\)\.$"),
            m => $"\"{m.Groups[1].Value}\" registered on its own (pending approval)."),
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
