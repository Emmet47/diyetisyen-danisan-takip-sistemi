# mobile — Danışan uygulaması (Flutter)

Repoda yalnızca bizim yazdığımız kod (`lib/`, `test/`, `pubspec.yaml`) var.
Android/iOS platform klasörleri her bilgisayarda Flutter'ın kendisi tarafından üretilir.

## İlk kurulum (her kişi bir kez yapar)

1. Flutter SDK'nın kurulu olduğunu kontrol edin:
   ```bash
   flutter doctor
   ```
2. Platform klasörlerini üretin (mevcut `lib/`, `test/`, `pubspec.yaml` dosyalarının üzerine yazmaz):
   ```bash
   cd mobile
   flutter create --project-name ddts_mobile --org com.ddts --platforms android,ios .
   ```
   iOS derlemek için macOS gerekir; Windows'ta yalnızca Android çalışır.
3. Geliştirme sırasında API'ye düz `http` ile bağlanabilmek için
   `android/app/src/debug/AndroidManifest.xml` dosyasına `application` satırını ekleyin:
   ```xml
   <manifest xmlns:android="http://schemas.android.com/apk/res/android">
       <uses-permission android:name="android.permission.INTERNET"/>
       <application android:usesCleartextTraffic="true" />
   </manifest>
   ```
   (Yalnızca debug derlemesini etkiler; yayın sürümü HTTPS kullanır.)
4. Paketleri indirin:
   ```bash
   flutter pub get
   ```
5. Üretilen `android/` ve `ios/` klasörlerini commit'leyin; ikinci kişi 2. adımı atlayıp doğrudan `flutter pub get` yapar.

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

## Klasör düzeni (önerilen)

```
lib/
├── core/        # API adresi, HTTP istemcisi, oturum (JWT) saklama, tema
├── features/    # modül başına klasör: auth/, meals/ (bugünkü öğünler, öğün kaydı + fotoğraf),
│                #   appointments/, notifications/
└── main.dart
```

Firebase (FCM) dosyaları (`google-services.json`, `GoogleService-Info.plist`) repo herkese açık olduğu
için `.gitignore`'da; ekip içinde ayrı paylaşılır.
