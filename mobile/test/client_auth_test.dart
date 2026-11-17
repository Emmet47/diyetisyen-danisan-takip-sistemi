import 'dart:convert';

import 'package:diyetisyen_danisan_mobil/core/api_client.dart';
import 'package:diyetisyen_danisan_mobil/features/auth/client_auth.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';

void main() {
  test('Danışan profili girişten dönen JWT ile alınır', () async {
    final expiresAt = DateTime.now().toUtc().add(const Duration(hours: 1));
    final requests = <String>[];
    final api = ApiClient(
      baseUrl: 'https://ddts.example',
      createClient: () => MockClient((request) async {
        requests.add('${request.method} ${request.url.path}');
        if (request.url.path == '/api/auth/login') {
          expect(jsonDecode(request.body), {
            'email': 'client@example.test',
            'password': 'test-password',
          });
          return http.Response(
            jsonEncode({
              'accessToken': 'test-token',
              'expiresAt': expiresAt.toIso8601String(),
              'role': 'Client',
            }),
            200,
          );
        }
        expect(request.headers['Authorization'], 'Bearer test-token');
        return http.Response(
          jsonEncode({'fullName': 'Örnek Danışan'}),
          200,
          headers: {'content-type': 'application/json; charset=utf-8'},
        );
      }),
    );

    final session = await signInClient(
      'client@example.test',
      'test-password',
      api: api,
    );

    expect(requests, ['POST /api/auth/login', 'GET /api/me/profile']);
    expect(session.fullName, 'Örnek Danışan');
    expect(session.expiresAt, expiresAt);
  });

  test('Diyetisyen hesabıyla danışan profili istenmez', () async {
    final api = ApiClient(
      baseUrl: 'https://ddts.example',
      createClient: () => MockClient((request) async {
        expect(request.url.path, '/api/auth/login');
        return http.Response('{"role":"Dietitian"}', 200);
      }),
    );

    await expectLater(
      signInClient('dietitian@example.test', 'test-password', api: api),
      throwsA(
        isA<ApiException>().having(
          (error) => error.message,
          'mesaj',
          contains('danışan hesabınızla'),
        ),
      ),
    );
  });
}
