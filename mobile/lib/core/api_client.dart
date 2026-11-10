import 'package:dio/dio.dart';

import 'api_config.dart';

/// Tüm API çağrılarında kullanılan ortak dio istemcisi.
/// Giriş (JWT) eklendiğinde token, burada bir interceptor ile her isteğe
/// `Authorization: Bearer <token>` başlığı olarak eklenecek.
final Dio apiClient = Dio(
  BaseOptions(
    baseUrl: ApiConfig.baseUrl,
    connectTimeout: const Duration(seconds: 5),
    receiveTimeout: const Duration(seconds: 10),
  ),
);
