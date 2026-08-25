# ProcureLive — B2B E-Tedarik ve Anlık İhale Sistemi

Faz 1: Solution iskeleti, tam veritabanı şeması, Auth/Company/Catalog/Currency modülleri.
Plan: `C:\Users\dogan\.claude\plans\keen-drifting-phoenix.md`

## Gereksinimler (bu makinede eksik olanlar)

Bu ortamda **Docker** ve **git** CLI kurulu değildi, bu yüzden aşağıdaki adımlar sizin tarafınızda tamamlanmalı:

1. [Docker Desktop](https://www.docker.com/products/docker-desktop/) kurun (Windows + WSL2 backend).
2. [Git for Windows](https://git-scm.com/download/win) kurun.
3. PowerShell'i yeniden başlatın (PATH güncellensin diye).

## Çalıştırma Adımları

```powershell
cd C:\Users\dogan\source\repos\ProcureLive

# 1) Postgres + Redis + pgAdmin'i ayağa kaldır
copy .env.example .env   # şifreleri değiştirmeyi unutmayın
docker compose up -d
docker compose ps        # hepsi "healthy" olmalı

# 2) dotnet-ef aracı (eğer kurulu değilse)
dotnet tool install --global dotnet-ef --version 8.0.10
$env:PATH += ";$env:USERPROFILE\.dotnet\tools"

# 3) Migration'ları veritabanına uygula (3 migration: InitialCreate, AddAnalyticsViews, SeedReferenceData)
dotnet ef database update `
  --project backend\src\Infrastructure\ProcureLive.Infrastructure.csproj `
  --startup-project backend\src\WebApi\ProcureLive.WebApi.csproj `
  --context ApplicationDbContext

# 4) API'yi çalıştır
dotnet run --project backend\src\WebApi\ProcureLive.WebApi.csproj
# Swagger: https://localhost:xxxx/swagger  (port için launchSettings.json'a bakın)
# Health check: /health

# 5) Git deposunu başlat (opsiyonel)
git init
git add -A
git commit -m "Faz 1: solution iskeleti, tam DB şeması, Auth/Company/Catalog/Currency modülleri"
```

## Faz 1'de Neler Var

- **Solution yapısı**: `ProcureLive.Domain`, `.Application`, `.Infrastructure`, `.WebApi`, `.Contracts` + 3 test projesi (Clean Architecture).
- **Veritabanı şeması (tam sistem)**: Identity/RBAC, Cari (Company/ContactPerson/Address), Katalog (Category/Product), Para Birimi (Currency/ExchangeRate), İhale, RFI, RFQ, Bildirim, Audit Log, AI feature store — hepsi migration'da mevcut.
- **Analytics view'ları**: `AuctionAnalyticsView`, `SupplierPerformanceView` (Faz 4 AI altyapısı için erken hazırlanmış).
- **Seed data**: Admin/Buyer/Supplier rolleri, TRY/USD/EUR para birimleri, kök kategori.
- **Çalışan modüller (API uçları)**: Auth (login/register/refresh/revoke/me), Users, Companies (+contacts/addresses), Categories, Products (+images), Currencies (+exchange rates).
- **RBAC**: JWT + resource-based authorization (`CompanyAccessHandler`) — Supplier kullanıcılar sadece kendi Company kayıtlarını görebilir.
- **Audit Log**: `AuditableEntitySaveChangesInterceptor` ile her Create/Update/Delete otomatik loglanır.
- Auction/RFI/RFQ/Notification **tabloları ve domain entity'leri hazır**, ancak CQRS handler/controller'ları Faz 2-3'te eklenecek (bkz. plan dosyası, Bölüm 8).

## İlk Admin Kullanıcısı

`register` uç noktası `RequireInternalUser` politikasıyla korunduğu için (tavuk-yumurta sorunu), uygulama ilk açılışta `InitialAdminSeeder` ile idempotent şekilde bir Admin kullanıcısı oluşturur:

- E-posta/parola `appsettings.json → InitialAdmin` bölümünden okunur (varsayılan: `admin@procurelive.local` / `ChangeMe123!`).
- **Üretimde mutlaka değiştirin** (user-secrets veya ortam değişkeni ile override edin).
- Kullanıcı zaten varsa seeder hiçbir şey yapmaz; migration'lar uygulandıktan sonra `dotnet run` her çalıştığında güvenle tekrar tetiklenebilir.

Bu Admin ile `/api/auth/login` üzerinden JWT alıp diğer `RequireInternalUser`/`RequireAdmin` uçlarına (örn. `register`, `companies`, `products`) erişebilirsiniz.

## Notlar

- JWT secret `dotnet user-secrets` içinde saklanıyor (appsettings.json'da yok).
- `.editorconfig`, `TryParse` gibi metotlarda inline `out` değişken tanımını belgeler (kod standardı).
- AutoMapper 13.0.1 için NU1903 uyarısı görünüyor (lisans bildirimi kaynaklı, gerçek bir CVE değil) — isterseniz daha yeni bir sürüme geçilebilir.
