import '../../core/api_client.dart';

class ClientSession {
  const ClientSession({
    required this.accessToken,
    required this.expiresAt,
    required this.fullName,
  });

  final String accessToken;
  final DateTime expiresAt;
  final String fullName;
}

Future<ClientSession> signInClient(
  String email,
  String password, {
  ApiClient api = const ApiClient(),
}) async {
  final result = await api.post('/api/auth/login', {
    'email': email,
    'password': password,
  });

  if (result['role'] != 'Client') {
    throw const ApiException('Bu uygulamaya danışan hesabınızla giriş yapın.');
  }
  final token = result['accessToken'];
  final expiration = result['expiresAt'];
  final expiresAt = expiration is String ? DateTime.tryParse(expiration) : null;
  if (token is! String || token.isEmpty || expiresAt == null) {
    throw const ApiException('Sunucu geçerli bir oturum bilgisi döndürmedi.');
  }
  if (!expiresAt.isAfter(DateTime.now())) {
    throw const ApiException('Oturum süresi dolmuş. Yeniden giriş yapın.');
  }
  final profile = await api.get('/api/me/profile', token: token);
  final name = profile['fullName'];
  if (name is! String || name.trim().isEmpty) {
    throw const ApiException('Danışan profili okunamadı.');
  }
  return ClientSession(
    accessToken: token,
    expiresAt: expiresAt,
    fullName: name.trim(),
  );
}
