# Diyetisyen Danışan Takip Sistemi (DDTS)

Diyetisyenlerin danışan profillerini, ölçümlerini, beslenme planlarını, randevularını ve gelişim raporlarını
tek platformdan yönettiği; danışanların mobil uygulamadan öğün kaydı ve fotoğraf yüklediği sistem.
Gereksinim analizi ve tasarım belgesi: `docs/` klasörü.

## Proje yapısı

```
Ddts.sln
├── src/
│   ├── Ddts.Domain/          # varlıklar, değer nesneleri, iş kuralları, domain event'ler
│   │                         # Composite, Builder, Strategy, Prototype (hiçbir pakete bağlı değil)
│   ├── Ddts.Application/     # use case servisleri, port arayüzleri, event handler'lar
│   ├── Ddts.Infrastructure/  # EF Core, USDA, SeaweedFS (S3), FCM, SMTP, QuestPDF, sahte ödeme
│   └── Ddts.Api/             # REST controller'lar, JWT, ReminderWorker, Program.cs (+ Dockerfile)
├── tests/
│   ├── Ddts.UnitTests/
│   ├── Ddts.IntegrationTests/
│   └── Ddts.ArchitectureTests/   # AT-01: katman bağımlılık kuralı
├── web/                      # React: diyetisyen + yönetici paneli (rol bazlı sayfalar)
├── mobile/                   # Flutter: danışan uygulaması
├── docs/                     # adr/, uml/, db/, api.md
├── .github/workflows/ci.yml  # CI: her PR'da derleme + testler
└── docker-compose.yml        # api, web, postgres, seaweedfs, mailpit
```

**Bağımlılık kuralı:** Domain hiçbir projeye bağlı değil; Application yalnızca Domain'e; Infrastructure ve Api, Application'a bağlı.
Hangi porta hangi adapter'ın takılacağını yalnızca `src/Ddts.Api/Program.cs` (composition root) seçer.

Klasör rehberleri: [web](web/README.md) · [web API rehberi](web/API_REHBERI.md) · [mobile](mobile/README.md) · [docs](docs/README.md)

## Gerekenler

| Araç | Ne için |
| --- | --- |
| Git | Kaynak kod |
| Docker Desktop (Windows'ta WSL2 altyapısıyla) | Veritabanı, fotoğraf deposu, e-posta ve demo ortamı |
| .NET 10 SDK | Backend |
| Node.js 24 veya 26 (LTS) | Web |
| Flutter (stable) + Android Studio (emülatör için) | Mobil |

Yalnızca web'de çalışan biri için Docker + Node.js yeterli.

## İlk kurulum

1. Repoyu klonlayın:
   ```bash
   git clone https://github.com/Emmet47/diyetisyen-danisan-takip-sistemi.git
   cd diyetisyen-danisan-takip-sistemi
   ```
2. Ortam değişkenleri dosyasını oluşturun ve **boş değerleri doldurun** (`.env` git'e gitmez):
   ```bash
   cp .env.example .env
   ```
   `POSTGRES_PASSWORD` ve `S3_SECRET_KEY` için istediğiniz bir değer, `JWT_SIGNING_KEY` için `openssl rand -base64 48` çıktısı.
3. Her şeyi Docker ile açın:
   ```bash
   docker compose up --build
   ```
4. http://localhost:3000 adresinde "API durumu: Healthy" yazıyorsa web → API zinciri çalışıyor.
5. Mobil için bir kerelik ek adımlar: [mobile/README.md](mobile/README.md).

## Adresler

| Ne | Adres |
| --- | --- |
| Web paneli (Docker) | http://localhost:3000 |
| Web paneli (`npm run dev`) | http://localhost:5173 |
| API deneme arayüzü (Scalar) | http://localhost:8080/scalar |
| OpenAPI belgesi | http://localhost:8080/openapi/v1.json |
| Sağlık kontrolü | http://localhost:8080/api/health |
| Mailpit gelen kutusu (sahte e-postalar) | http://localhost:8025 |
| PostgreSQL (DBeaver / pgAdmin ile) | `localhost:5432`, kullanıcı ve parola `.env` içinde |
| SeaweedFS S3 (fotoğraf deposu) | http://localhost:8333 |
| Android emülatöründen API | http://10.0.2.2:8080 |

## Backend'de çalışmak

### API'yi bilgisayarda çalıştırmak (kaydedince yeniden başlar)

1. Altyapıyı Docker'da açın: `docker compose up -d postgres seaweedfs mailpit`
2. Gizli ayarları bir kez `dotnet user-secrets` ile girin (repo herkese açık; parolalar dosyaya yazılmaz). Değerler `.env` ile aynı olmalı:
   ```bash
   dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=ddts;Username=ddts;Password=<POSTGRES_PASSWORD>" --project src/Ddts.Api
   dotnet user-secrets set "Storage:AccessKey" "ddts-dev" --project src/Ddts.Api
   dotnet user-secrets set "Storage:SecretKey" "<S3_SECRET_KEY>" --project src/Ddts.Api
   dotnet user-secrets set "Jwt:SigningKey" "<JWT_SIGNING_KEY>" --project src/Ddts.Api
   dotnet user-secrets set "Usda:ApiKey" "<USDA anahtarı, varsa>" --project src/Ddts.Api
   ```
3. Çalıştırın: `dotnet watch --project src/Ddts.Api` (veya Visual Studio / Rider'dan) → http://localhost:8080/scalar

### Yeni bir özellik eklerken (örnek: "danışan ekle")

1. **Domain**: `Client` varlığı ve kuralları (`src/Ddts.Domain/Clients/`).
2. **Application**: `IClientRepository` portu ve `ClientService` use case'i (`src/Ddts.Application/Clients/`).
3. **Infrastructure**: EF eşlemesi (`Persistence/Configurations/ClientConfiguration.cs`) ve `EfClientRepository`.
4. **Api**: `Controllers/ClientsController.cs`; action'lara `///` yorumu yazın, Scalar'da açıklama olarak görünür.
5. **Program.cs**: yeni servis ve adapter kayıtları (`AddScoped<IClientRepository, EfClientRepository>()` ...).
6. **Migration** ekleyin (aşağıda).
7. **Testler**: iş kuralları `tests/Ddts.UnitTests`, uç nokta senaryoları (yetkisiz erişim 404, çakışma 409 ...) `tests/Ddts.IntegrationTests`.

### Migration (EF Core ile veritabanı şeması)

```bash
dotnet tool restore     # dotnet-ef aracını kurar (bir kez)
dotnet ef migrations add IlkSema --project src/Ddts.Infrastructure --startup-project src/Ddts.Api --output-dir Persistence/Migrations
```

- Development ortamında API açılırken bekleyen migration'lar kendiliğinden uygulanır (`Database:MigrateOnStartup`).
- Tablo ve sütun adları otomatik snake_case olur (EFCore.NamingConventions).
- Veritabanının asıl kaynağı migration'lardır; `docs/db/schema.sql` tasarım belgesidir. Migration yazınca kısıtların (FK, CHECK, UNIQUE, EXCLUDE) onunla aynı olduğunu kontrol edin.
- Aynı anda yalnızca bir kişi migration ekler (paralel iki migration birleşirken çakışır).

### Testler

Testler xUnit v3 ile yeni test altyapısında (Microsoft Testing Platform) çalışır; bu mod `global.json`'da açık.

```bash
dotnet test --solution Ddts.sln                  # hepsi (entegrasyon testleri için Docker açık olmalı)
dotnet test --project tests/Ddts.UnitTests       # yalnızca birim testleri
```

### NuGet paketi eklemek

Sürümler tek yerde: `Directory.Packages.props` (merkezi paket yönetimi; iki kişi aynı paketin farklı sürümünü eklemesin diye).

1. `Directory.Packages.props` içine `<PackageVersion Include="Paket.Adi" Version="x.y.z" />` ekleyin.
2. İlgili `.csproj` dosyasına sürümsüz `<PackageReference Include="Paket.Adi" />` ekleyin.
3. Paketi belgedeki "11.1 Kütüphaneler ve araçlar" tablosuna da yazın.

## Git iş akışı

1. `main` dalı korumalı; doğrudan `main`'e push yapılmaz.
2. Her iş önce GitHub'da bir **issue** (görev kaydı) olur; numarası commit'lerde kullanılır.
3. Her iş ayrı dalda yapılır:
   ```bash
   git switch main
   git pull
   git switch -c feature/plan-builder     # hata düzeltmesi: fix/..., ayar/altyapı: chore/...
   ```
4. Commit mesajları kısa ve kurallı: `tür(kapsam): ne yapıldı (#issue)`
   - `feat(plans): builder ile plan oluşturma (#12)`
   - `test(db): çift randevu kısıt testi (#18)`
   - Türler: `feat` yeni özellik, `fix` hata düzeltme, `test`, `docs`, `ci`, `build`, `chore` (ayar/altyapı).
5. Dal push'lanır, GitHub'da pull request (PR: "değişikliğimi main'e almak istiyorum" isteği) açılır; açıklamaya `Closes #12` yazılır.
6. CI'daki dört kontrol yeşil olur ve bir ekip arkadaşı inceleyip onaylar → **Squash and merge**.
7. Çakışmaya açık dosyalar: EF Core migration'ları (aynı anda tek kişi), `Directory.Packages.props` ve `web/package-lock.json` (paket eklerken haber verin).

### GitHub ayarları (repo sahibi bir kez yapar)

1. **Settings → Collaborators**: ekip arkadaşlarını ekleyin.
2. **Settings → Rules → Rulesets → New ruleset → New branch ruleset**:
   - Ad: `main-koruma`, Enforcement status: **Active**, Target branches: **Include default branch**.
   - **Require a pull request before merging** açık, **Required approvals** = 1.
   - **Require status checks to pass** açık; CI bir kez çalıştıktan sonra şu dört kontrolü ekleyin:
     `Backend (.NET)`, `Web (React)`, `Mobil (Flutter)`, `Docker Compose`.
   - **Block force pushes** açık.
3. **Settings → General → Pull Requests**: yalnızca "Allow squash merging" açık, "Automatically delete head branches" açık.
4. **Projects** sekmesi: görev panosu (To do / In progress / Done) açıp issue'ları oraya bağlayın.

## CI (`.github/workflows/ci.yml`)

| İş | Ne yapar |
| --- | --- |
| Backend (.NET) | Derleme; birim, mimari (AT-01) ve entegrasyon testleri; test raporu (.trx) indirilebilir |
| Web (React) | Lint, TypeScript kontrolü, üretim derlemesi |
| Mobil (Flutter) | `flutter analyze`, `flutter test` |
| Docker Compose | Tüm sistemi açıp web → API zincirini dener |
