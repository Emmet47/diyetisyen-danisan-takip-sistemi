import 'dart:convert';

import 'package:diyetisyen_danisan_mobil/core/api_client.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';

/// Testlerde gerçek API'nin yerine geçen sahte sunucu. Yanıt şekilleri
/// docs/api.md ile aynıdır; veriler sahtedir.
class FakeBackend {
  final requests = <String>[];
  final bodies = <String, Object?>{};

  int measurementStatus = 200;
  double weight = 71.65;
  String? mealStatus;
  bool slotTaken = false;
  final appointments = <Map<String, Object?>>[];
  Map<String, Object?> settings = {
    'appointments': true,
    'mealReminders': true,
    'measurements': true,
    'plans': true,
  };

  static const today = '2026-11-17';
  static const slotStart = '2026-11-18T07:00:00Z';

  static const totals = {
    'energyKcal': 400,
    'proteinG': 20,
    'carbsG': 50,
    'fatG': 12,
    'proteinPercent': 20,
    'carbsPercent': 50,
    'fatPercent': 27,
  };

  static const breakfast = {
    'id': 'meal-1',
    'dayNo': 1,
    'name': 'Kahvaltı',
    'time': '08:00:00',
    'totals': totals,
    'items': [
      {
        'id': 'item-1',
        'foodItemId': 'food-1',
        'foodName': 'Yulaf ezmesi',
        'portionName': '1 yemek kaşığı',
        'portionCount': 4,
        'grams': 40,
        'isAlternative': false,
        'alternativeToItemId': null,
        'totals': totals,
      },
    ],
  };

  ApiClient get api => ApiClient(
    baseUrl: 'https://ddts.example',
    createClient: () => MockClient(_handle),
  );

  Future<http.Response> _handle(http.Request request) async {
    final path = request.url.path;
    final key = '${request.method} $path';
    requests.add(key);
    if (path != '/api/auth/login' && path != '/api/auth/forgot-password') {
      expect(request.headers['Authorization'], 'Bearer test-token');
    }
    if (request.body.isNotEmpty && request.method != 'GET') {
      bodies[key] = jsonDecode(request.body);
    }
    switch (key) {
      case 'POST /api/auth/login':
        return _json({
          'accessToken': 'test-token',
          'expiresAt': DateTime.now()
              .toUtc()
              .add(const Duration(hours: 1))
              .toIso8601String(),
          'role': 'Client',
        });
      case 'POST /api/auth/forgot-password':
        return http.Response('', 202);
      case 'GET /api/me/profile':
        return _json({'fullName': 'Örnek Danışan'});
      case 'GET /api/me/meals/today':
        return _json({
          'date': today,
          'planId': 'plan-1',
          'planTitle': 'Deneme planı',
          'meals': [
            {
              'meal': breakfast,
              'log': mealStatus == null
                  ? null
                  : {
                      'id': 'log-1',
                      'mealId': 'meal-1',
                      'logDate': today,
                      'status': mealStatus,
                      'note': 'Afiyetle',
                      'hasPhoto': false,
                      'loggedAt': '2026-11-17T06:00:00Z',
                    },
            },
          ],
        });
      case 'GET /api/me/plan':
        return _json({
          'id': 'plan-1',
          'clientId': 'client-1',
          'title': 'Deneme planı',
          'planType': 'Daily',
          'status': 'Published',
          'targetKcal': 1800,
          'energyFormula': 'MifflinStJeor',
          'sourceTemplateId': null,
          'createdAt': '2026-11-10T08:00:00Z',
          'publishedAt': '2026-11-11T08:00:00Z',
          'archivedAt': null,
          'meals': [breakfast],
          'days': [
            {'dayNo': 1, 'totals': totals},
          ],
          'totals': totals,
          'disclaimer':
              'Hesaplamalar tahminidir, profesyonel değerlendirme yerine geçmez.',
        });
      case 'GET /api/me/meal-logs':
        return _json([]);
      case 'POST /api/me/meal-logs':
        mealStatus = (bodies[key] as Map)['status'] as String;
        return _json({'id': 'log-1'}, 201);
      case 'GET /api/me/measurements':
        if (measurementStatus != 200) {
          return _json({'title': 'Hata', 'status': measurementStatus}, measurementStatus);
        }
        return _json({
          'targetWeightKg': 68,
          'remainingKg': weight - 68,
          'items': [
            {
              'measuredOn': '2026-06-01',
              'weightKg': 72.4,
              'heightCm': 170,
              'waistCm': 84,
            },
            {
              'measuredOn': '2026-06-09',
              'weightKg': weight,
              'heightCm': 170,
              'waistCm': 83,
              'weightChangeKg': weight - 72.4,
              'waistChangeCm': -1,
              'note': 'Kontrol ölçümü',
            },
          ],
        });
      case 'GET /api/me/appointments':
        return _json(appointments);
      case 'GET /api/me/available-slots':
        return _json([
          if (appointments.isEmpty)
            {'startsAt': slotStart, 'endsAt': '2026-11-18T07:30:00Z'},
        ]);
      case 'POST /api/me/appointments':
        if (slotTaken) {
          slotTaken = false;
          return _json({
            'title': 'Bu saat az önce doldu',
            'status': 409,
            'detail': 'Lütfen başka bir saat seç.',
          }, 409);
        }
        final created = {
          'id': 'appt-1',
          'clientId': 'client-1',
          'clientName': 'Örnek Danışan',
          'startsAt': slotStart,
          'endsAt': '2026-11-18T07:30:00Z',
          'status': 'Requested',
          'note': null,
        };
        appointments.add(created);
        return _json(created, 201);
      case 'GET /api/notifications':
        return _json({
          'items': [
            {
              'id': 'n-1',
              'type': 'PlanPublished',
              'title': 'Yeni planın hazır',
              'body': 'Diyetisyenin yeni bir plan yayımladı.',
              'isRead': false,
              'createdAt': '2026-11-17T06:00:00Z',
            },
          ],
          'page': 1,
          'pageSize': 50,
          'totalCount': 1,
        });
      case 'POST /api/notifications/n-1/read':
        return http.Response('', 204);
      case 'GET /api/me/notification-settings':
        return _json(settings);
      case 'PUT /api/me/notification-settings':
        settings = Map<String, Object?>.from(bodies[key] as Map);
        return http.Response('', 204);
    }
    return _json({'title': 'Bulunamadı', 'status': 404}, 404);
  }

  static http.Response _json(Object? body, [int status = 200]) =>
      http.Response.bytes(
        utf8.encode(jsonEncode(body)),
        status,
        headers: {'content-type': 'application/json; charset=utf-8'},
      );
}
