import 'dart:convert';

import 'package:diyetisyen_danisan_mobil/core/api_client.dart';
import 'package:flutter/material.dart';
import 'package:diyetisyen_danisan_mobil/main.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:http/testing.dart';

Future<void> openClientPanel(WidgetTester tester) async {
  await tester.pumpWidget(const DanisanTakipApp());
  final demo = find.text('Örnek verileri incele');
  await tester.ensureVisible(demo);
  await tester.tap(demo);
  await tester.pumpAndSettle();
}

void main() {
  testWidgets(
    'Danışan girişten sonra ölçüm geçmişini sunucudan okur ve yeniler',
    (tester) async {
      await tester.binding.setSurfaceSize(const Size(390, 850));
      addTearDown(() => tester.binding.setSurfaceSize(null));
      final requests = <String>[];
      var measurementStatus = 200;
      var weight = 71.65;
      final api = ApiClient(
        baseUrl: 'https://ddts.example',
        createClient: () => MockClient((request) async {
          requests.add('${request.method} ${request.url.path}');
          if (request.url.path != '/api/auth/login') {
            expect(request.headers['Authorization'], 'Bearer test-token');
          }
          final response = switch (request.url.path) {
            '/api/auth/login' => {
              'accessToken': 'test-token',
              'expiresAt': DateTime.now()
                  .toUtc()
                  .add(const Duration(hours: 1))
                  .toIso8601String(),
              'role': 'Client',
            },
            '/api/me/profile' => {'fullName': 'Örnek Danışan'},
            '/api/me/measurements' => {
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
            },
            _ => fail('Beklenmeyen istek ${request.url}'),
          };
          return http.Response.bytes(
            utf8.encode(jsonEncode(response)),
            request.url.path == '/api/me/measurements'
                ? measurementStatus
                : 200,
          );
        }),
      );
      await tester.pumpWidget(DanisanTakipApp(api: api));
      await tester.enterText(
        find.byType(TextFormField).first,
        'client@example.test',
      );
      await tester.enterText(find.byType(TextFormField).last, 'test-password');
      final login = find.text('Giriş yap');
      await tester.ensureVisible(login);
      await tester.tap(login);
      await tester.pumpAndSettle();
      await tester.tap(find.text('Gelişim'));
      await tester.pumpAndSettle();

      expect(requests, [
        'POST /api/auth/login',
        'GET /api/me/profile',
        'GET /api/me/measurements',
      ]);
      expect(find.text('71,65 kg'), findsOneWidget);
      expect(find.text('Hedefe fark 3,65 kg'), findsOneWidget);
      expect(find.textContaining('Kontrol ölçümü'), findsOneWidget);
      await tester.tap(find.text('Bel çevresi'));
      await tester.pumpAndSettle();
      expect(find.text('83 cm'), findsOneWidget);

      measurementStatus = 503;
      await tester.tap(find.byTooltip('Ölçümleri yenile'));
      await tester.pumpAndSettle();
      expect(find.text('Tekrar dene'), findsOneWidget);
      expect(find.text('83 cm'), findsNothing);

      measurementStatus = 200;
      weight = 71;
      await tester.tap(find.text('Tekrar dene'));
      await tester.pumpAndSettle();
      await tester.tap(find.text('Kilo'));
      await tester.pumpAndSettle();
      expect(find.text('71 kg'), findsOneWidget);
      expect(find.text('Hedefe fark 3 kg'), findsOneWidget);

      measurementStatus = 401;
      await tester.tap(find.byTooltip('Ölçümleri yenile'));
      await tester.pumpAndSettle();
      expect(find.text('Danışan girişi'), findsOneWidget);
    },
  );

  testWidgets('Besin aranır, porsiyonuyla kaydedilir ve yeniden açılır', (
    tester,
  ) async {
    await tester.binding.setSurfaceSize(const Size(390, 850));
    addTearDown(() => tester.binding.setSurfaceSize(null));
    await openClientPanel(tester);
    await tester.tap(find.text('Kahvaltı'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Besin ekle'));
    await tester.pumpAndSettle();

    await tester.enterText(find.byType(TextField), 'cig badem');
    await tester.pumpAndSettle();
    expect(find.text('Yoğurt'), findsNothing);
    await tester.tap(find.text('Çiğ badem'));
    await tester.pumpAndSettle();
    await tester.enterText(find.byType(TextFormField), '0');
    await tester.tap(find.text('Öğüne ekle'));
    await tester.pumpAndSettle();
    expect(find.text('0,01 ile 20 arasında porsiyon girin.'), findsOneWidget);
    await tester.enterText(find.byType(TextFormField), '1,5');
    await tester.pumpAndSettle();
    expect(find.text('37,5 g · 217 kcal'), findsOneWidget);
    await tester.tap(find.text('Öğüne ekle'));
    await tester.pumpAndSettle();
    expect(find.text('1,5 porsiyon · 37,5 g · 217 kcal'), findsOneWidget);

    await tester.scrollUntilVisible(
      find.text('Öğünü kaydet'),
      120,
      scrollable: find
          .descendant(
            of: find.byType(ListView),
            matching: find.byType(Scrollable),
          )
          .first,
    );
    await tester.ensureVisible(find.text('Öğünü kaydet'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Öğünü kaydet'));
    await tester.pumpAndSettle();
    expect(find.textContaining('1.417 kcal alındı'), findsOneWidget);

    await tester.tap(find.text('Kahvaltı'));
    await tester.pumpAndSettle();
    expect(find.text('Çiğ badem'), findsOneWidget);
  });

  testWidgets('Diyet listem öğünleri ve porsiyonları görüntüler', (
    tester,
  ) async {
    await tester.binding.setSurfaceSize(const Size(390, 850));
    addTearDown(() => tester.binding.setSurfaceSize(null));
    await openClientPanel(tester);

    await tester.tap(find.text('Diyet listem'));
    await tester.pumpAndSettle();
    expect(find.text('Günlük beslenme planı'), findsOneWidget);
    expect(find.text('Örnek plan · 4 öğün · 1.800 kcal'), findsOneWidget);
    expect(find.text('Yulaf'), findsOneWidget);
    expect(find.text('4 yemek kaşığı'), findsWidgets);
    expect(find.text('Öğünü kaydet'), findsNothing);
  });

  testWidgets('Porsiyon düzenlenince kalori özeti ve günlük güncellenir', (
    tester,
  ) async {
    await tester.binding.setSurfaceSize(const Size(390, 850));
    addTearDown(() => tester.binding.setSurfaceSize(null));
    await openClientPanel(tester);

    await tester.tap(find.text('Kahvaltı'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Kısmen yedim'));
    await tester.pumpAndSettle();
    await tester.scrollUntilVisible(
      find.text('Öğünü kaydet'),
      120,
      scrollable: find
          .descendant(
            of: find.byType(ListView),
            matching: find.byType(Scrollable),
          )
          .first,
    );
    await tester.pumpAndSettle();
    expect(find.text('Fotoğraf çek'), findsOneWidget);
    await tester.ensureVisible(find.text('Öğünü kaydet'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Öğünü kaydet'));
    await tester.pumpAndSettle();

    expect(find.textContaining('975 kcal alındı'), findsOneWidget);
    await tester.scrollUntilVisible(
      find.text('105 / 200 g'),
      120,
      scrollable: find
          .descendant(
            of: find.byType(ListView),
            matching: find.byType(Scrollable),
          )
          .first,
    );
    expect(find.text('105 / 200 g'), findsOneWidget);
    await tester.tap(find.text('Günlük'));
    await tester.pumpAndSettle();
    expect(find.text('Kısmen yedim'), findsOneWidget);
    expect(find.text('08.30 · 225 kcal'), findsOneWidget);
  });

  testWidgets('Randevu isteği oluşturulur ve iptal edilebilir', (tester) async {
    await tester.binding.setSurfaceSize(const Size(390, 850));
    addTearDown(() => tester.binding.setSurfaceSize(null));
    await openClientPanel(tester);
    await tester.tap(find.text('Randevular'));
    await tester.pumpAndSettle();

    await tester.tap(find.text('10:30'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Randevu talep et'));
    await tester.pumpAndSettle();
    expect(find.text('Talep edildi'), findsOneWidget);

    final cancel = find.text('İptal et').last;
    await tester.ensureVisible(cancel);
    await tester.tap(cancel);
    await tester.pumpAndSettle();
    await tester.tap(find.text('İptal et').last);
    await tester.pumpAndSettle();
    expect(find.text('İptal edildi'), findsOneWidget);
  });
}
