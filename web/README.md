# DDTS web paneli

Diyetisyen paneli ve platform yöneticisi paneli (React + Vite). Danışanlar web paneline giremez, mobil uygulamayı kullanır.

## Yerelde açma

Node.js kurulu olmalı. API (.NET) ve altyapı çalışıyor olmalı (kök README → "İlk kurulum"). Terminalde, `web` klasöründe çalıştır:

```powershell
npm.cmd ci
npm.cmd run dev -- --strictPort
```

Tarayıcıda `http://localhost:5173` adresini aç. Hazır demo verisi yok: hesabı "Hesap oluştur" ile açıp verileri sahte bilgilerle gir (NFR-D1).

## API ile çalışma

- Panel, `/api` isteklerini Vite proxy'si (aradaki yönlendirici) üzerinden `http://localhost:8080`'deki .NET API'ye iletir. API başka adresteyse: `DDTS_API_URL=http://localhost:5080 npm run dev`. Docker'da aynı işi `nginx.conf` yapar; bu yüzden CORS (tarayıcının başka porttaki API'ye istek atma izni) gerekmez.
- Bütün API çağrıları `src/api/` klasöründe: ortak istemci `http.ts` (token ekleme, hata metinleri, 401'de girişe dönme), her modül için bir dosya (`clients.ts`, `plans.ts`, `appointments.ts` ...). Tipler `models.ts`'te ve backend DTO adlarıyla aynı.
- JWT (giriş token'ı) 60 dakika geçerli, yenileme anahtarı yok: süre dolunca ya da API 401 dönünce giriş ekranına dönülür.
- Davet ve parola sıfırlama e-postaları `http://localhost:8025` (Mailpit) gelen kutusuna düşer; bağlantı bu paneldeki `/reset-password` sayfasını açar. API'deki `Web:BaseUrl` ayarı panelin adresi olmalı: Vite ile `http://localhost:5173`, Docker ile `http://localhost:3000`.
- Randevu saatleri API'de UTC, ekranda Türkiye saati (UTC+3). Öğün ve çalışma saatleri zaten Türkiye saati (`"08:00:00"`).
- PDF rapor token gerektirdiği için düz bağlantıyla değil, token'lı istekle indirilir (`api.download`).
- Öğün fotoğrafı "Fotoğrafı aç" ile 10 dakikalık imzalı bağlantıdan panelde pencere olarak açılır.
- Plan düzenleyicide miktar gram olarak girilir; API'ye porsiyon sayısı gider (gram ÷ besinin porsiyon gramı, en fazla 20 porsiyon). Haftalık planda günler yan yana sütun olarak gösterilir.
- Bildirime tıklayınca okundu olur ve ilgili sayfa açılır (randevu → Randevular, öğün → Öğün günlüğü ...).

## Sayfalar

| Menü | Modül | Uç noktalar |
| --- | --- | --- |
| Genel bakış | özet | clients, subscription, plans, appointments, measurements |
| Danışanlar | B (sd-1) | /api/clients, restrictions, history, activate/deactivate |
| Ölçümler ve gelişim | C + K (sd-6) | measurements, progress, report.pdf |
| Beslenme planları / Plan şablonları | E + F (sd-2, sd-3, sd-4) | /api/plans, publish, archive, /api/templates, copy-to-client |
| Plan şablonları → "Hazır şablonları ekle" | F | 20 örnek şablon (`src/presetTemplates.ts`) ortak besinlerle POST /api/templates üzerinden eklenir |
| Besin kataloğu | D (sd-8) | /api/foods, import |
| Öğün günlüğü | G (sd-5) | clients/{id}/meal-logs, meal-logs/{id}/photo-url |
| Enerji hesabı | H | /api/energy/calculate |
| Randevular | I (sd-7) | /api/working-hours, /api/appointments |
| Bildirimler | J (sd-4) | /api/notifications |
| Aboneliğim | L | /api/subscription |
| Yönetici sayfaları | A/L | /api/admin/* |

## Kontroller

```powershell
npm.cmd run lint     # TypeScript kontrolü (CI'da da çalışır)
npm.cmd run build    # üretim derlemesi
```
