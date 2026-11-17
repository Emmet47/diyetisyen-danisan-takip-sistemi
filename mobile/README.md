# mobile — Danışan uygulaması (Flutter)

Danışanın öğünlerini, ölçümlerini, randevularını ve bildirimlerini takip ettiği uygulama. Bütün veriler .NET API'den gelir.

## İlk kurulum (her kişi bir kez yapar)

1. Flutter SDK'nın kurulu olduğunu kontrol edin:
   ```bash
   flutter doctor
   ```
2. `android/` klasörü repoda hazır (debug derlemesinde düz `http` izni `android/app/src/debug/AndroidManifest.xml` içinde açık).
   iOS derlemek için macOS gerekir; Windows'ta yalnızca Android çalışır.
3. Paketleri indirin:
   ```bash
   cd mobile
   flutter pub get
   ```

## Çalıştırma

- Android emülatörü (API bilgisayarda `localhost:8080`'de açıkken):
  ```bash
  flutter run
  ```
  Emülatör bilgisayara `10.0.2.2` adresiyle ulaşır; bu varsayılan olarak ayarlı (`lib/core/api_config.dart`).
- Gerçek telefon (aynı Wi-Fi'da, bilgisayarın yerel IP'si ile):
  ```bash
  flutter run --dart-define=API_BASE_URL=http://192.168.1.20:8080
  ```

## Komutlar

| Komut | Ne yapar |
| --- | --- |
| `flutter analyze` | Kod denetimi (`analysis_options.yaml`) |
| `flutter test` | Widget testleri (`test/`) |

## Uygulamada neler var

| Ekran | Modül | Uç noktalar |
| --- | --- | --- |
| Giriş, "Şifremi unuttum" | A (UC-02, UC-03) | /api/auth/login, /api/auth/forgot-password, /api/me/profile |
| KVKK aydınlatma metni | gereksinimler 2.5 | girişten sonra her seferinde gösterilir |
| Bugün, Diyet listem | E (UC-25, FR-E5) | /api/me/meals/today, /api/me/plan |
| Öğün kaydı + fotoğraf, Günlük | G (UC-26, UC-27, sd-5) | /api/me/meal-logs, /api/me/meal-logs/{id}/photo, /api/meal-logs/{id}/photo-url |
| Gelişim | C (UC-28, FR-C4) | /api/me/measurements |
| Randevular | I (UC-29, UC-30, sd-7) | /api/me/available-slots, /api/me/appointments |
| Bildirimler ve izinler | J (UC-31) | /api/notifications, /api/me/notification-settings |

- Danışan hesabını diyetisyen açar; davet e-postasındaki bağlantı web'deki `/reset-password` sayfasını açar, parola orada belirlenir.
- JWT (giriş token'ı) 60 dakika geçerli, yenileme anahtarı yok: süre dolunca ya da API 401 dönünce giriş ekranına dönülür.
- Randevu saatleri API'de UTC, ekranda telefonun saat dilimine bakılmadan Türkiye saati (UTC+3) gösterilir.
- Hata mesajları API'nin ProblemDetails `detail` alanından gelir (ör. "Bu saat az önce doldu. Lütfen başka bir saat seç.").
- Öğün fotoğrafı: yalnızca JPEG/PNG, en fazla 5 MB, kayıt başına bir tane. Görüntüleme bağlantısı sunucudaki `STORAGE_PUBLIC_URL`
  adresiyle üretilir; emülatör ya da telefonda fotoğrafı görmek için `.env`'de bu değere bilgisayarın yerel IP'si yazılmalı
  (ör. `http://192.168.1.20:8333`), sonra `docker compose up -d api`.
- Telefona anlık bildirim (push, FCM): API tarafı hazır (`PUT /api/me/push-token`), ancak uygulama tarafı için Firebase paketleri
  (`firebase_core`, `firebase_messaging`) ve `google-services.json` gerekir; bu sürümde eklenmedi. Bildirimler uygulama içinde
  listelenir ve dört izin anahtarı sunucuya kaydedilir.
- Uygulamada koda gömülü örnek veri yok (NFR-D1); veriler web panelinden sahte bilgilerle girilir.

## Klasör düzeni

```
lib/
├── core/        # API adresi, HTTP istemcisi (ApiClient + SessionApi), JSON okuma, tarih/sayı biçimleri
├── features/    # modül başına API ve modeller: auth/, meals/, appointments/, notifications/, measurements/
├── screens/     # ekranlar: bugün/günlük/öğün kaydı, gelişim, randevular, bildirimler
└── main.dart    # oturum kapısı (giriş → KVKK → ana ekran)
test/            # widget testleri; fake_backend.dart sahte sunucu (MockClient)
```

Firebase (FCM) dosyaları (`google-services.json`, `GoogleService-Info.plist`) repo herkese açık olduğu
için `.gitignore`'da; ekip içinde ayrı paylaşılır.
