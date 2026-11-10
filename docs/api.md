# 8. REST API ve Rol-Yetki Matrisi

## 8.1 Genel kurallar

- Adres biçimi `https://<sunucu>/api/…`. Veri JSON (UTF-8); tarih-saatler ISO 8601 ve UTC (ör. `2026-12-01T07:00:00Z`, yani Türkiye saatiyle 10.00).
- Girişte alınan JWT her istekte `Authorization: Bearer <token>` başlığıyla gönderiliyor.
- Hatalar ProblemDetails biçiminde dönüyor (RFC 9457 standardındaki `application/problem+json`): `type`, `title`, `status`, `detail` ve gerekirse alan hatalarını gösteren `errors`.
- Uzun listeler sayfalı: `?page=1&pageSize=20`.
- API dokümantasyonu ASP.NET Core'un OpenAPI desteğiyle otomatik üretiliyor (`/openapi/v1.json`); geliştirme ortamında Scalar arayüzüyle tarayıcıdan denenebiliyor.

| Durum kodu | Ne zaman dönüyor? |
| --- | --- |
| 200 OK | Başarılı okuma ya da işlem |
| 201 Created | Yeni kayıt oluştu (`Location` başlığıyla) |
| 202 Accepted | İstek alındı (parola sıfırlama e-postası) |
| 204 No Content | Gövdesi olmayan başarılı işlem (ör. silme) |
| 400 Bad Request | Geçersiz girdi ya da iş kuralı ihlali: eksik alan, alerjen besin, yanlış dosya türü |
| 401 Unauthorized | Token yok, geçersiz ya da süresi dolmuş; e-posta ya da parola hatalı |
| 403 Forbidden | Giriş yapılmış ama kullanıcının rolü bu işlemi yapamaz |
| 404 Not Found | Kayıt yok ya da kullanıcıya ait değil (IDOR koruması) |
| 409 Conflict | Mevcut durumla çakışma: paket limiti dolu, slot dolu, aynı gün ikinci ölçüm, taslak olmayan planı düzenleme |
| 429 Too Many Requests | Giriş denemesi sınırı aşıldı |

## 8.2 Uç noktalar

**Kimlik ve herkese açık uçlar**

| Metot | Yol | Rol | Açıklama |
| --- | --- | --- | --- |
| POST | /api/auth/register | Anonim | Diyetisyen hesabı açar ve paket seçer (FR-A1) |
| POST | /api/auth/login | Anonim | JWT döner (FR-A2) |
| POST | /api/auth/forgot-password | Anonim | Sıfırlama e-postası gönderir; e-posta kayıtlı olsun olmasın hep 202 döner (FR-A3) |
| POST | /api/auth/reset-password | Anonim | Bağlantıdaki token ile yeni parola belirler; danışan daveti de bununla tamamlanır |
| GET | /api/subscription-plans | Anonim | Satıştaki paketler (kayıt ekranı için) |

**Diyetisyen: danışan, ölçüm ve rapor**

| Metot | Yol | Rol | Açıklama |
| --- | --- | --- | --- |
| GET · POST | /api/clients | Diyetisyen | Kendi danışanları (`?active=true`) / danışan ekleme, limit kontrolüyle (FR-B1, FR-L2) |
| GET · PUT · DELETE | /api/clients/{id} | Diyetisyen | Profil / güncelleme, geçmişe yazılır (FR-B3) / kalıcı silme (FR-B4) |
| PUT | /api/clients/{id}/restrictions | Diyetisyen | Alerji/kısıt listesi (FR-B2) |
| GET | /api/clients/{id}/history | Diyetisyen | Değişiklik geçmişi |
| POST | /api/clients/{id}/deactivate, /activate | Diyetisyen | Pasife alma / limit kontrolüyle yeniden aktifleştirme |
| GET · POST | /api/clients/{id}/measurements | Diyetisyen | Ölçüm geçmişi / yeni ölçüm (FR-C1, FR-C2) |
| PUT · DELETE | /api/clients/{id}/measurements/{measurementId} | Diyetisyen | Ölçüm düzeltme / silme |
| GET | /api/clients/{id}/progress?from=&to= | Diyetisyen | İlerleme paneli verisi (FR-K1, FR-K3) |
| GET | /api/clients/{id}/report.pdf?from=&to= | Diyetisyen | PDF rapor (FR-K2) |
| GET | /api/clients/{id}/meal-logs?from=&to= | Diyetisyen | Planlanan–gerçekleşen öğünler (FR-G3) |
| GET | /api/meal-logs/{id}/photo-url | Diyetisyen, Danışan | 10 dakika geçerli fotoğraf bağlantısı (sahiplik kontrolüyle) |

**Diyetisyen: besin, enerji, plan ve şablon**

| Metot | Yol | Rol | Açıklama |
| --- | --- | --- | --- |
| GET | /api/foods?q=&source=&external= | Diyetisyen | Arama; `external=true` ise USDA da aranır (FR-D1, FR-D4) |
| POST | /api/foods | Diyetisyen | Yerel besin ekler (FR-D2) |
| PUT · DELETE | /api/foods/{id} | Diyetisyen | Yalnızca kendi kaydını düzenler/siler |
| POST | /api/foods/import | Diyetisyen | USDA sonucunu yerel veritabanına aktarır (FR-D3) |
| POST | /api/energy/calculate | Diyetisyen | BMH, günlük ihtiyaç ve hedef (FR-H1) |
| GET · POST | /api/plans | Diyetisyen | Planlar (`?clientId=`) / Builder ile yeni plan (FR-E1) |
| GET · PUT · DELETE | /api/plans/{id} | Diyetisyen | Plan ve toplamları; yalnız taslak düzenlenir ve silinir |
| POST | /api/plans/{id}/publish, /archive | Diyetisyen | Yayımlama (eskisi arşivlenir) / arşivleme (FR-E3, FR-E4) |
| GET · POST | /api/templates | Diyetisyen | Şablonlar (FR-F1) |
| GET · PUT · DELETE | /api/templates/{id} | Diyetisyen | Şablon işlemleri |
| POST | /api/templates/{id}/copy-to-client | Diyetisyen | Prototype ile bağımsız taslak plan (FR-F2) |

**Diyetisyen: randevu, abonelik ve bildirim**

| Metot | Yol | Rol | Açıklama |
| --- | --- | --- | --- |
| GET · PUT | /api/working-hours | Diyetisyen | Haftalık çalışma saatleri (FR-I1) |
| GET · POST | /api/appointments | Diyetisyen | Takvim (`?from=&to=`) / doğrudan onaylı randevu |
| POST | /api/appointments/{id}/confirm, /cancel, /reschedule, /complete | Diyetisyen | Durum değişiklikleri (FR-I2, FR-I3) |
| GET | /api/subscription | Diyetisyen | Paket, durum, bitiş tarihi ve kullanım (ör. 8/10) |
| POST | /api/subscription/change | Diyetisyen | Paket yükseltme/düşürme (FR-L3) |
| POST | /api/subscription/renew | Diyetisyen | Ödeme simülasyonuyla 1 ay uzatma (FR-L4) |
| GET | /api/notifications | Diyetisyen, Danışan | Kendi bildirimleri |
| POST | /api/notifications/{id}/read | Diyetisyen, Danışan | Okundu işaretleme |

**Danışan (mobil uygulama): /api/me**

| Metot | Yol | Rol | Açıklama |
| --- | --- | --- | --- |
| GET | /api/me/profile | Danışan | Kendi profili |
| GET | /api/me/plan | Danışan | Yayımlanmış planı (FR-E5) |
| GET | /api/me/meals/today | Danışan | Bugünkü öğünler ve kayıt durumları |
| GET · POST | /api/me/meal-logs | Danışan | Öğün kayıtları / yeni kayıt (FR-G1) |
| PUT | /api/me/meal-logs/{id} | Danışan | Kaydı günceller |
| POST | /api/me/meal-logs/{id}/photo | Danışan | Fotoğraf yükler, multipart/form-data (FR-G2) |
| GET | /api/me/measurements | Danışan | Ölçüm geçmişi (FR-C4) |
| GET | /api/me/available-slots?date= | Danışan | Boş slotlar (FR-I1) |
| GET · POST | /api/me/appointments | Danışan | Randevuları / randevu talebi (FR-I2) |
| POST | /api/me/appointments/{id}/reschedule, /cancel | Danışan | Erteleme / iptal (FR-I3) |
| PUT | /api/me/notification-settings | Danışan | Bildirim izinleri (FR-J2) |
| PUT | /api/me/push-token | Danışan | Telefonun FCM anahtarını kaydeder |

**Yönetici: /api/admin**

| Metot | Yol | Rol | Açıklama |
| --- | --- | --- | --- |
| GET | /api/admin/dietitians | Yönetici | Diyetisyenler, paketleri ve kullanımları |
| POST | /api/admin/dietitians/{id}/activate, /deactivate | Yönetici | Hesabı açma/kapatma (FR-L5) |
| GET · POST | /api/admin/subscription-plans | Yönetici | Paketler / yeni paket (FR-L1) |
| PUT | /api/admin/subscription-plans/{id} | Yönetici | Paketi düzenleme ya da satıştan kaldırma |
| GET | /api/admin/usage | Yönetici | Diyetisyen başına kullanım (x / limit) |
| GET | /api/admin/audit-logs?from=&to=&userId=&action= | Yönetici | Denetim kayıtları (ayrıntı alanı olmadan) |

## 8.3 Örnek istek ve yanıtlar

**1) Giriş**

```http
POST /api/auth/login
Content-Type: application/json

{ "email": "ayse.dyt@ornek.test", "password": "Demo!2026" }

HTTP/1.1 200 OK
{ "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...", "expiresAt": "2026-12-02T08:00:00Z", "role": "Dietitian" }
```

**2) Danışan ekleme: başarılı ve paket limiti dolu**

```http
POST /api/clients
Authorization: Bearer eyJ...
Content-Type: application/json

{
  "fullName": "Deneme Danışan",
  "email": "deneme.danisan@ornek.test",
  "phone": "05550000000",
  "birthDate": "1996-01-01",
  "sex": "Female",
  "goalType": "LoseWeight",
  "targetWeightKg": 62.0,
  "preferences": "Kahvaltıda tatlı sevmiyor",
  "allergyNotes": "Fıstık alerjisi",
  "kvkkConsentGiven": true
}

HTTP/1.1 201 Created
Location: /api/clients/7f8c2a1e-3b4d-4c5e-9f60-1a2b3c4d5e6f
{ "id": "7f8c2a1e-3b4d-4c5e-9f60-1a2b3c4d5e6f", "usage": { "activeClients": 9, "maxClients": 10, "nearLimit": true } }

HTTP/1.1 409 Conflict
Content-Type: application/problem+json
{
  "type": "https://ddts.example/errors/subscription-limit",
  "title": "Paket limiti dolu",
  "status": 409,
  "detail": "Başlangıç paketinde en fazla 10 aktif danışan olabilir. Paketini yükseltebilir ya da bir danışanı pasife alabilirsin."
}
```

**3) Enerji hesabı**

```http
POST /api/energy/calculate
{ "clientId": "7f8c2a1e-3b4d-4c5e-9f60-1a2b3c4d5e6f", "formula": "MifflinStJeor", "activityFactor": 1.55, "adjustmentKcal": -500 }

HTTP/1.1 200 OK
{
  "formula": "MifflinStJeor",
  "input": { "sex": "Female", "ageYears": 30, "weightKg": 70.0, "heightCm": 165.0 },
  "bmrKcal": 1420.25,
  "dailyNeedKcal": 2201.39,
  "targetKcal": 1701.39,
  "disclaimer": "Hesaplamalar tahminidir, profesyonel değerlendirme yerine geçmez."
}
```

**4) Şablonu danışana kopyalama**

```http
POST /api/templates/0b9d4e2a-8c1f-4a6b-b2d3-5e6f7a8b9c0d/copy-to-client
{ "clientId": "7f8c2a1e-3b4d-4c5e-9f60-1a2b3c4d5e6f", "title": "Kasım planı" }

HTTP/1.1 201 Created
Location: /api/plans/5c1e9b7a-2d3f-4e5a-8b6c-7d8e9f0a1b2c
{
  "planId": "5c1e9b7a-2d3f-4e5a-8b6c-7d8e9f0a1b2c",
  "status": "Draft",
  "sourceTemplateId": "0b9d4e2a-8c1f-4a6b-b2d3-5e6f7a8b9c0d",
  "warnings": [ "Fıstık ezmesi: danışanın alerji listesinde, yayımlamadan önce değiştirmelisin." ]
}
```

**5) Öğün fotoğrafı: yanlış dosya ve görüntüleme bağlantısı**

```http
POST /api/me/meal-logs/9a7b6c5d-4e3f-4a2b-9c1d-0e1f2a3b4c5d/photo
Content-Type: multipart/form-data; boundary=----ddts
(dosya: ogle.gif, 1,2 MB)

HTTP/1.1 400 Bad Request
Content-Type: application/problem+json
{ "title": "Geçersiz dosya", "status": 400, "detail": "Sadece JPEG/PNG, en fazla 5 MB yükleyebilirsin." }

GET /api/meal-logs/9a7b6c5d-4e3f-4a2b-9c1d-0e1f2a3b4c5d/photo-url

HTTP/1.1 200 OK
{
  "url": "https://depo.ddts.example/meal-photos/3f6d...jpg?X-Amz-Algorithm=AWS4-HMAC-SHA256&X-Amz-Expires=600&X-Amz-Signature=...",
  "expiresAt": "2026-12-02T08:10:00Z"
}
```

**6) Randevu talebi: başarılı ve slot dolu**

```http
POST /api/me/appointments
{ "startsAt": "2026-12-01T07:00:00Z" }

HTTP/1.1 201 Created
{ "id": "e2d4f6a8-1b3c-4d5e-8f70-9a1b2c3d4e5f", "status": "Requested", "startsAt": "2026-12-01T07:00:00Z", "endsAt": "2026-12-01T07:30:00Z" }

HTTP/1.1 409 Conflict
Content-Type: application/problem+json
{ "title": "Bu saat az önce doldu", "status": 409, "detail": "Lütfen başka bir saat seç." }
```

**7) Besin arama: dış kaynak çalışmıyor**

```http
GET /api/foods?q=yulaf&external=true

HTTP/1.1 200 OK
{
  "items": [
    { "id": "c41f2e3d-5a6b-4c7d-8e9f-0a1b2c3d4e5f", "name": "Yulaf ezmesi", "portionName": "1 yemek kaşığı", "portionGrams": 10,
      "energyKcalPer100g": 379, "proteinPer100g": 13.2, "carbsPer100g": 67.7, "fatPer100g": 6.5,
      "sourceName": "USDA FoodData Central", "sourceUpdatedAt": "2026-11-03T09:12:00Z" }
  ],
  "externalUnavailable": true,
  "message": "Dış kaynağa şu an ulaşılamıyor, yerel kayıtlar gösteriliyor."
}
```

**8) Yetkisiz erişim**

```http
GET /api/clients/7f8c2a1e-3b4d-4c5e-9f60-1a2b3c4d5e6f    (Diyetisyen B'nin token'ıyla)
HTTP/1.1 404 Not Found

GET /api/clients                                          (danışan token'ıyla)
HTTP/1.1 403 Forbidden

GET /api/clients                                          (token olmadan)
HTTP/1.1 401 Unauthorized
```

## 8.4 Rol-yetki matrisi

“✓” yapabilir, “✓ kendi” yalnızca kendi kaydı ya da kendi danışanı için yapabilir, “—” yapamaz (403) demek. Sahiplik ihlali (ör. başka diyetisyenin danışanı) 404 döner.

| İşlem | Diyetisyen | Danışan | Yönetici |
| --- | --- | --- | --- |
| Hesap açma | ✓ (kendisi) | — (diyetisyenin davetiyle) | — (ilk yönetici seed ile gelir) |
| Giriş, parola sıfırlama | ✓ | ✓ | ✓ |
| Danışan ekleme, güncelleme, pasife alma, silme | ✓ kendi | — | — |
| Danışan profilini görme | ✓ kendi | ✓ kendi | — |
| Değişiklik geçmişini görme | ✓ kendi | — | — |
| Ölçüm ekleme, düzeltme | ✓ kendi | — | — |
| Ölçüm geçmişi ve grafik | ✓ kendi | ✓ kendi | — |
| İlerleme paneli ve PDF rapor | ✓ kendi | — | — |
| Besin arama, dış kaynaktan aktarma | ✓ | — | — |
| Yerel besin düzenleme/silme | ✓ kendi (ortak kayıtlar hariç) | — | — |
| Enerji hesaplama | ✓ kendi | — | — |
| Plan oluşturma, düzenleme, yayımlama, arşivleme | ✓ kendi | — | — |
| Planı ve bugünkü öğünleri görme | ✓ kendi | ✓ kendi (yalnız yayımlanmış plan) | — |
| Şablon işlemleri ve danışana kopyalama | ✓ kendi | — | — |
| Öğün kaydı ve fotoğraf yükleme | — | ✓ kendi | — |
| Öğün kayıtlarını ve fotoğrafları görme | ✓ kendi | ✓ kendi | — |
| Çalışma saatlerini tanımlama | ✓ | — (yalnız boş slotları görür) | — |
| Randevu talep etme | — | ✓ | — |
| Randevu onaylama, oluşturma, tamamlama | ✓ kendi | — | — |
| Randevu erteleme, iptal | ✓ kendi | ✓ kendi | — |
| Bildirimleri görme | ✓ kendi | ✓ kendi | — |
| Bildirim izinlerini yönetme | — | ✓ kendi | — |
| Kendi aboneliğini görme, paket değiştirme, ödeme simülasyonu | ✓ | — | — |
| Paket tanımlama ve düzenleme | — | — | ✓ |
| Diyetisyen hesabını açma/kapatma | — | — | ✓ |
| Kullanım limitlerini izleme | ✓ (kendi kullanımı) | — | ✓ (tüm diyetisyenler, yalnız sayı olarak) |
| Denetim kayıtlarını görme | — | — | ✓ (sağlık verisi içeren ayrıntılar olmadan) |
