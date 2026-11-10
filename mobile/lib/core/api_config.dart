/// API adresi uygulama derlenirken verilir:
///   flutter run --dart-define=API_BASE_URL=http://192.168.1.20:8080
/// Verilmezse Android emülatörünün bilgisayara ulaştığı adres (10.0.2.2) kullanılır.
class ApiConfig {
  static const String baseUrl = String.fromEnvironment(
    'API_BASE_URL',
    defaultValue: 'http://10.0.2.2:8080',
  );
}
