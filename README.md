# ProcureLive — B2B E-Tedarik ve Anlık İhale Sistemi

Kurumsal satınalma ekipleri için B2B e-tedarik ve canlı ihale platformu. Alıcılar ihale, RFI ve RFQ açar,
onaylı tedarikçileri davet eder, gerçek zamanlı ihale yürütür (İngiliz, Hollanda, kapalı zarf), teklifleri
karşılaştırır ve kazananları satınalma siparişine dönüştürür. Tedarikçiler kayıt olur, davetleri yanıtlar,
canlı teklif verir ve siparişleri onaylar. Backend .NET 8 (Clean Architecture, CQRS), frontend Nuxt 3.

## Gereksinimler

Docker Desktop (+ WSL2), Git, .NET 8 SDK ve Node.js 20+ gereklidir.

## Çalıştırma Adımları

```powershell
git clone https://github.com/dogandincer/ProcureLive.git
cd ProcureLive

# 1) Postgres + Redis + pgAdmin'i ayağa kaldır
copy .env.example .env   # şifreleri değiştirmeyi unutmayın
docker compose up -d
docker compose ps        # hepsi "healthy"/"Up" olmalı

# 2) dotnet-ef aracı (eğer kurulu değilse)
dotnet tool install --global dotnet-ef --version 8.0.10
$env:PATH += ";$env:USERPROFILE\.dotnet\tools"

# 3) Migration'ları veritabanına uygula
dotnet ef database update `
  --project backend\src\Infrastructure\ProcureLive.Infrastructure.csproj `
  --startup-project backend\src\WebApi\ProcureLive.WebApi.csproj `
  --context ApplicationDbContext

# 4) Backend API'yi çalıştır (Swagger: /swagger, Health: /health)
dotnet run --project backend\src\WebApi\ProcureLive.WebApi.csproj --urls "http://localhost:5228"

# 5) Frontend'i çalıştır (ayrı bir terminalde)
cd frontend
npm install
npm run dev -- --port 3000
# http://localhost:3000/login
```

## İlk Admin Kullanıcısı

Uygulama ilk açılışta `InitialAdminSeeder` ile idempotent şekilde bir Admin kullanıcısı oluşturur
(`register` uç noktası `RequireInternalUser` gerektirdiği için tavuk-yumurta sorununu çözer):

- E-posta/parola `appsettings.json → InitialAdmin` bölümünden okunur (varsayılan: `admin@procurelive.local` / `ChangeMe123!`).
- **Üretimde mutlaka değiştirin** (user-secrets veya ortam değişkeni ile override edin).

## Mimari Özet

### Backend (`backend/`) — Clean Architecture
- **Domain**: entity'ler, value object'ler (`Money`), enum'lar — hiçbir paket bağımlılığı yok.
- **Application**: CQRS (MediatR) — Companies, Catalog, Finance, Auctions, Rfi, Rfq, Notifications, Ai modülleri; ortak davranışlar (validation/logging/authorization pipeline), `CurrencyNormalizer` (çoklu para birimi karşılaştırma).
- **Infrastructure**: EF Core + Npgsql, ASP.NET Identity, Redis cache, JWT, e-posta stub'ı, AI öneri placeholder'ı.
- **WebApi**: Controller'lar, `AuctionHub`/`NotificationHub` (SignalR), arka plan servisleri (ihale yaşam döngüsü, e-posta dağıtımı).

### Frontend (`frontend/`) — Nuxt 3 + Tailwind
- `app/pages/admin/*` — Admin Dashboard, Cariler, Stok/Ürünler, İhale yönetimi (yayınla/davet/süre uzatma/kazanan seçme), RFI/RFQ süreç yönetimi.
- `app/pages/supplier/*` — Cari Paneli, ihale/RFI/RFQ davetleri (kabul/red), **Canlı İhale Ekranı** (gerçek zamanlı teklif, SignalR).
- `app/composables/` — `useAuth` (JWT), `useApi` (fetch wrapper), `useAuctionHub`/`useNotificationHub` (SignalR).

## Veritabanı ve Gerçek Zamanlı Altyapı

- **Tam şema tek seferde kuruldu**: Identity/RBAC, Cari, Katalog, Finans, İhale, RFI, RFQ, Bildirim, Audit Log, AI feature store.
- **Analytics view'ları**: `AuctionAnalyticsView`, `SupplierPerformanceView` — AI öneri servisi bunları kullanır.
- **RBAC**: JWT + resource-based authorization (`CompanyAccessHandler`, `AuctionAccessHandler`) — Supplier'lar yalnızca kendi Cari'lerini ve davetli oldukları (Invited dahil, Declined hariç) ihale/RFI/RFQ'ları görebilir; teklif verme ayrıca ve daha sıkı kontrol edilir (yalnızca Accepted/Participating).
- **Audit Log**: `AuditableEntitySaveChangesInterceptor` ile her Create/Update/Delete otomatik loglanır.
- **SignalR**: `AuctionHub` (canlı teklif/süre uzatma/kazanan bildirimleri) ve `NotificationHub` (bildirim merkezi anlık push) — her ikisi de Redis backplane kullanır, sunucu tarafında grup üyeliği yetkilendirmesi yapılır.
- **AI önerisi**: `IAuctionModelRecommender` arayüzü + `HeuristicAuctionModelRecommender` (kural tabanlı placeholder) — gerçek bir model eklendiğinde yalnızca bu Infrastructure sınıfı değişecek.

## Notlar

- JWT secret `dotnet user-secrets` içinde saklanıyor (appsettings.json'da yok).
- `.editorconfig`, `TryParse` gibi metotlarda inline `out` değişken tanımını belgeler (kod standardı).
- AutoMapper 13.0.1 için NU1903 uyarısı görünüyor (lisans bildirimi kaynaklı, gerçek bir CVE değil).
- Frontend `nuxt.config.ts` içinde `srcDir: "app/"` kullanır; Tailwind içerik taraması `tailwind.config.ts`'de bu yapıya göre açıkça tanımlıdır.
- E-posta: Admin, **Sistem → E-posta Ayarları** ekranından SMTP sunucusunu (Gmail / Microsoft 365 / Yandex hazır ayarlarıyla), gönderen bilgisini ve hangi bildirimlerin e-postayla gideceğini belirler; formdaki ayarla test e-postası gönderilebilir. Bildirimler arka planda kuyruktan gönderilir (hatada 1 ve 5 dk sonra yeniden denenir), gönderim kayıtları aynı ekrandadır. Kullanıcı kendi e-postalarını Profil ekranından kapatabilir. SMTP şifresi ASP.NET Data Protection ile şifrelenir; sunucuya taşırken `DataProtection:KeysPath` ile kalıcı anahtar klasörü verin. Ayar kapalıyken e-postalar yalnızca loglanır.
- Geliştirmede gerçek e-posta göndermeden denemek için `docker compose up -d mailpit`: ayarlarda "Geliştirme (Mailpit)" hazır ayarını seçin, gelen kutusu http://localhost:8025.

## Lisans

MIT — ayrıntılar için [LICENSE](LICENSE) dosyasına bakın.
