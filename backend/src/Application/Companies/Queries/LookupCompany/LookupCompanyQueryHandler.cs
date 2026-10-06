using MediatR;
using ProcureLive.Contracts.Companies;

namespace ProcureLive.Application.Companies.Queries.LookupCompany;

/// <summary>
/// TCKN/VKN ile cari sorgulama (SİMÜLASYON). Numara uzunluk + resmi checksum algoritması ile
/// GERÇEKTEN doğrulanır; doğrulanan numaradan deterministik, makul mock cari bilgisi üretilir.
/// Gerçek NVİ/GİB entegrasyonu kurumsal kimlik/erişim gerektirir; e-posta ve AI 3D gibi burası da
/// arayüz arkasında bir stand-in — ileride gerçek sağlayıcı yalnızca bu handler'ı değiştirir.
/// </summary>
public sealed class LookupCompanyQueryHandler : IRequestHandler<LookupCompanyQuery, CompanyLookupResultDto>
{
    private static readonly string[] NamePrefixes =
        { "Anadolu", "Ege", "Marmara", "Toros", "Yıldız", "Kuzey", "Doğuş", "Zirve", "Mercan", "Akdeniz", "Bereket", "Nova", "Öz", "Birlik", "Güven" };

    private static readonly string[] NameCores =
        { "Tedarik", "Lojistik", "Metal", "Tekstil", "Gıda", "İnşaat", "Teknoloji", "Makine", "Enerji", "Kimya", "Ambalaj", "Otomotiv", "Elektrik", "Mobilya", "Tarım" };

    private static readonly string[] LegalSuffixes = { "Sanayi ve Ticaret A.Ş.", "Ltd. Şti.", "Tic. A.Ş.", "San. Tic. Ltd. Şti." };

    private static readonly (string City, string District, string TaxOffice)[] Locations =
    {
        ("İstanbul", "Beşiktaş", "Beşiktaş V.D."),
        ("İstanbul", "Ümraniye", "Ümraniye V.D."),
        ("Ankara", "Çankaya", "Çankaya V.D."),
        ("İzmir", "Bornova", "Bornova V.D."),
        ("Bursa", "Nilüfer", "Nilüfer V.D."),
        ("Kocaeli", "Gebze", "Gebze V.D."),
        ("Konya", "Selçuklu", "Selçuklu V.D."),
        ("Gaziantep", "Şehitkamil", "Şehitkamil V.D."),
    };

    private static readonly string[] Streets = { "Atatürk Cad.", "Cumhuriyet Bul.", "İnönü Sok.", "Organize Sanayi Böl.", "Fatih Cad.", "Barbaros Bul." };

    public Task<CompanyLookupResultDto> Handle(LookupCompanyQuery request, CancellationToken cancellationToken)
    {
        string id = (request.Identifier ?? string.Empty).Trim();

        if (id.Length == 11 && id.All(char.IsDigit))
        {
            if (IsValidTckn(id))
                return Task.FromResult(BuildResult(id, "TCKN"));
            return Task.FromResult(Invalid(id, "TCKN", "Geçersiz TCKN (algoritma doğrulaması başarısız)."));
        }

        if (id.Length == 10 && id.All(char.IsDigit))
        {
            if (IsValidVkn(id))
                return Task.FromResult(BuildResult(id, "VKN"));
            return Task.FromResult(Invalid(id, "VKN", "Geçersiz VKN (algoritma doğrulaması başarısız)."));
        }

        return Task.FromResult(Invalid(id, "Unknown", "TCKN 11 haneli, VKN 10 haneli ve yalnızca rakamlardan oluşmalıdır."));
    }

    private static CompanyLookupResultDto Invalid(string id, string type, string message) =>
        new(id, type, false, null, null, null, null, null, null, null, null, null, null, message);

    private static CompanyLookupResultDto BuildResult(string id, string type)
    {
        // Deterministic mock data derived from the (validated) number.
        int seed = 0;
        foreach (char c in id) seed = seed * 31 + (c - '0');
        seed = Math.Abs(seed);

        string prefix = NamePrefixes[seed % NamePrefixes.Length];
        string core = NameCores[(seed / 7) % NameCores.Length];
        string suffix = LegalSuffixes[(seed / 13) % LegalSuffixes.Length];
        var loc = Locations[(seed / 17) % Locations.Length];
        string street = Streets[(seed / 23) % Streets.Length];
        int buildingNo = (seed % 180) + 1;
        int foundationYear = 1975 + (seed % 48);

        string shortName = $"{prefix} {core}";
        string tradeName = $"{prefix} {core} {suffix}";
        // MERSİS: 16 hane → "0" + 10 haneli VKN + 5 haneli üretilmiş ek (yalnızca VKN için).
        string? mersis = type == "VKN" ? $"0{id}{seed % 100000:D5}" : null;

        string slug = (prefix + core).ToLowerInvariant()
            .Replace("ı", "i").Replace("ş", "s").Replace("ğ", "g")
            .Replace("ü", "u").Replace("ö", "o").Replace("ç", "c");

        return new CompanyLookupResultDto(
            Identifier: id,
            IdentifierType: type,
            IsValid: true,
            Name: shortName,
            TradeName: tradeName,
            TaxOffice: loc.TaxOffice,
            MersisNo: mersis,
            City: loc.City,
            District: loc.District,
            AddressLine: $"{street} No:{buildingNo}",
            Phone: $"+90 {200 + (seed % 332)} {100 + (seed % 900)} {1000 + (seed % 9000)}",
            Email: $"info@{slug}.com.tr",
            FoundationYear: foundationYear,
            Message: "Sorgu başarılı (simülasyon verisi).");
    }

    private static bool IsValidTckn(string t)
    {
        if (t.Length != 11 || t[0] == '0') return false;
        int[] d = t.Select(c => c - '0').ToArray();
        int oddSum = d[0] + d[2] + d[4] + d[6] + d[8];
        int evenSum = d[1] + d[3] + d[5] + d[7];
        int tenth = (((oddSum * 7) - evenSum) % 10 + 10) % 10;
        if (tenth != d[9]) return false;
        int sumFirstTen = d.Take(10).Sum();
        return sumFirstTen % 10 == d[10];
    }

    private static bool IsValidVkn(string v)
    {
        if (v.Length != 10) return false;
        int[] d = v.Select(c => c - '0').ToArray();
        int sum = 0;
        for (int i = 0; i < 9; i++)
        {
            int tmp = (d[i] + (9 - i)) % 10;
            int mod = (tmp * (1 << (9 - i))) % 9;
            int prod = (tmp != 0 && mod == 0) ? 9 : mod;
            sum += prod;
        }
        int check = (10 - (sum % 10)) % 10;
        return check == d[9];
    }
}
