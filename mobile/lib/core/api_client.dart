import 'dart:async';
import 'dart:convert';

import 'package:flutter/foundation.dart';
import 'package:http/http.dart' as http;

import 'api_config.dart';

/// API'den dönen hata. [message] ekranda gösterilecek Türkçe metindir.
class ApiException implements Exception {
  const ApiException(this.message, {this.statusCode});

  final String message;
  final int? statusCode;

  @override
  String toString() => message;
}

/// Sunucu ProblemDetails içinde "detail" göndermezse kullanılan Türkçe metinler.
String _statusMessage(int status, {required bool signedIn}) => switch (status) {
  400 => 'Girdiğiniz bilgileri kontrol edin.',
  401 =>
    signedIn
        ? 'Oturumunuz sona erdi. Yeniden giriş yapın.'
        : 'E-posta veya şifre hatalı.',
  403 => 'Bu işlem için yetkiniz yok.',
  404 => 'Kayıt bulunamadı.',
  409 => 'Bu işlem kaydın şu anki durumuyla çakışıyor. Tekrar deneyin.',
  429 => 'Çok fazla deneme yapıldı. Biraz sonra tekrar deneyin.',
  503 => 'Hizmete şu an ulaşılamıyor. Biraz sonra tekrar deneyin.',
  _ => 'Sunucu işlemi tamamlayamadı. Tekrar deneyin.',
};

/// Tek HTTP istemcisi: bütün ekranlar API'ye bunun üzerinden gider.
/// Adres `--dart-define=API_BASE_URL=...` ile verilir (varsayılan: Android
/// emülatöründen bilgisayardaki API, `http://10.0.2.2:8080`).
class ApiClient {
  const ApiClient({
    this.baseUrl = ApiConfig.baseUrl,
    this.createClient = http.Client.new,
  });

  final String baseUrl;
  final http.Client Function() createClient;

  /// Giriş gerektiren GET; gövde JSON nesnesi olmalı.
  Future<Map<String, dynamic>> get(String path, {required String token}) async {
    final json = await send('GET', path, token: token);
    if (json is Map<String, dynamic>) return json;
    throw const ApiException('Sunucudan beklenmeyen bir yanıt geldi.');
  }

  /// Giriş gerektirmeyen POST (giriş, parola sıfırlama).
  Future<Map<String, dynamic>> post(
    String path,
    Map<String, String> data,
  ) async {
    final json = await send('POST', path, body: data);
    if (json is Map<String, dynamic>) return json;
    throw const ApiException('Sunucudan beklenmeyen bir yanıt geldi.');
  }

  /// Genel istek. Yanıt gövdesi boşsa null, değilse çözülmüş JSON döner.
  /// Hatalar ProblemDetails biçimindedir; "detail" alanı (ör. "Bu saat az önce
  /// doldu") olduğu gibi gösterilir.
  Future<Object?> send(
    String method,
    String path, {
    String? token,
    Object? body,
  }) async {
    final request = http.Request(method, _resolve(path))
      ..followRedirects = false;
    request.headers['Accept'] = 'application/json';
    if (token != null) request.headers['Authorization'] = 'Bearer $token';
    if (body != null) {
      request.headers['Content-Type'] = 'application/json; charset=utf-8';
      request.body = jsonEncode(body);
    }
    return _run(request, signedIn: token != null);
  }

  /// Dosya yükleme (multipart/form-data). Alan adı API'de "file".
  Future<Object?> upload(
    String path, {
    required String token,
    required Uint8List bytes,
    required String filename,
  }) async {
    final request = http.MultipartRequest('POST', _resolve(path))
      ..followRedirects = false;
    request.headers['Accept'] = 'application/json';
    request.headers['Authorization'] = 'Bearer $token';
    request.files.add(
      http.MultipartFile.fromBytes('file', bytes, filename: filename),
    );
    return _run(request, signedIn: true);
  }

  Uri _resolve(String path) {
    final base = Uri.tryParse(baseUrl);
    if (base == null ||
        !base.hasAuthority ||
        (base.scheme != 'https' && (base.scheme != 'http' || !kDebugMode))) {
      throw const ApiException('API adresini kontrol edin.');
    }
    return base.resolve(path);
  }

  Future<Object?> _run(
    http.BaseRequest request, {
    required bool signedIn,
  }) async {
    final client = createClient();
    try {
      Future<http.Response> exchange() async =>
          http.Response.fromStream(await client.send(request));
      final response = await exchange().timeout(const Duration(seconds: 20));
      final text = utf8.decode(response.bodyBytes);
      if (response.statusCode < 200 || response.statusCode > 299) {
        throw ApiException(
          _problemDetail(text, response.statusCode) ??
              _statusMessage(response.statusCode, signedIn: signedIn),
          statusCode: response.statusCode,
        );
      }
      if (text.trim().isEmpty) return null;
      return jsonDecode(text);
    } on TimeoutException {
      throw const ApiException('Sunucu yanıt vermedi. Tekrar deneyin.');
    } on http.ClientException {
      throw const ApiException(
        'Sunucuya bağlanılamadı. Bağlantınızı kontrol edin.',
      );
    } on FormatException {
      throw const ApiException('Sunucudan beklenmeyen bir yanıt geldi.');
    } finally {
      client.close();
    }
  }

  static String? _problemDetail(String text, int status) {
    if (text.trim().isEmpty) return null;
    try {
      final json = jsonDecode(text);
      if (json is Map<String, dynamic>) {
        final detail = json['detail'];
        final title = json['title'];
        if (detail is String && detail.trim().isNotEmpty) {
          // 409'da başlık da anlamlı: "Bu saat az önce doldu. Lütfen başka
          // bir saat seç."
          if (status == 409 && title is String && title != 'Çakışma') {
            return '$title. $detail';
          }
          return detail;
        }
      }
    } on FormatException {
      return null;
    }
    return null;
  }
}

/// Giriş yapılmış danışanın istekleri: token'ı ekler, 401 gelirse oturumu
/// kapatır (JWT 60 dakika geçerli, yenileme anahtarı yok → girişe dönülür).
class SessionApi {
  const SessionApi({
    required this.api,
    required this.token,
    required this.onUnauthorized,
  });

  final ApiClient api;
  final String token;
  final VoidCallback onUnauthorized;

  Future<Object?> get(String path) =>
      _guard(() => api.send('GET', path, token: token));

  Future<Object?> post(String path, [Object? body]) =>
      _guard(() => api.send('POST', path, token: token, body: body));

  Future<Object?> put(String path, Object body) =>
      _guard(() => api.send('PUT', path, token: token, body: body));

  Future<Object?> upload(String path, Uint8List bytes, String filename) =>
      _guard(
        () => api.upload(
          path,
          token: token,
          bytes: bytes,
          filename: filename,
        ),
      );

  Future<Object?> _guard(Future<Object?> Function() call) async {
    try {
      return await call();
    } on ApiException catch (error) {
      if (error.statusCode == 401) onUnauthorized();
      rethrow;
    }
  }
}
