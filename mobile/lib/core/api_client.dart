import 'dart:async';
import 'dart:convert';

import 'package:flutter/foundation.dart';
import 'package:http/http.dart' as http;

import 'api_config.dart';

class ApiException implements Exception {
  const ApiException(this.message, {this.statusCode});

  final String message;
  final int? statusCode;
}

class ApiClient {
  const ApiClient({
    this.baseUrl = ApiConfig.baseUrl,
    this.createClient = http.Client.new,
  });

  final String baseUrl;
  final http.Client Function() createClient;

  Future<Map<String, dynamic>> get(String path, {required String token}) =>
      _request(path, token: token);

  Future<Map<String, dynamic>> post(String path, Map<String, String> data) =>
      _request(path, data: data);

  Future<Map<String, dynamic>> _request(
    String path, {
    String? token,
    Map<String, String>? data,
  }) async {
    final base = Uri.tryParse(baseUrl);
    if (base == null ||
        !base.hasAuthority ||
        (base.scheme != 'https' && (base.scheme != 'http' || !kDebugMode))) {
      throw const ApiException('API adresini kontrol edin.');
    }

    final request = http.Request(
      data == null ? 'GET' : 'POST',
      base.resolve(path),
    )..followRedirects = false;
    request.headers['Accept'] = 'application/json';
    if (token != null) request.headers['Authorization'] = 'Bearer $token';
    if (data != null) {
      request.headers['Content-Type'] = 'application/json; charset=utf-8';
      request.body = jsonEncode(data);
    }

    final client = createClient();
    try {
      return await (() async {
        final response = await http.Response.fromStream(
          await client.send(request),
        );
        if (response.statusCode != 200) {
          throw ApiException(switch (response.statusCode) {
            400 => 'Girdiğiniz bilgileri kontrol edin.',
            401 =>
              token == null
                  ? 'E-posta veya şifre hatalı.'
                  : 'Oturumunuz sona erdi. Yeniden giriş yapın.',
            403 => 'Bu işlem için yetkiniz yok.',
            429 => 'Çok fazla deneme yapıldı. Biraz sonra tekrar deneyin.',
            _ => 'Sunucu işlemi tamamlayamadı. Tekrar deneyin.',
          }, statusCode: response.statusCode);
        }
        final json = jsonDecode(utf8.decode(response.bodyBytes));
        if (json is Map<String, dynamic>) return json;
        throw const ApiException('Sunucudan beklenmeyen bir yanıt geldi.');
      })().timeout(const Duration(seconds: 10));
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
}
