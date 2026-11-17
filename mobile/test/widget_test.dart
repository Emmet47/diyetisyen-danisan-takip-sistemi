import 'package:diyetisyen_danisan_mobil/main.dart';
import 'package:diyetisyen_danisan_mobil/screens/meals.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:material_ui/material_ui.dart';

import 'fake_backend.dart';

/// Giriş yapar ve KVKK aydınlatma metnini onaylar.
Future<void> signIn(WidgetTester tester, FakeBackend backend) async {
  await tester.binding.setSurfaceSize(const Size(390, 850));
  addTearDown(() => tester.binding.setSurfaceSize(null));
  await tester.pumpWidget(DanisanTakipApp(api: backend.api));
  await tester.enterText(
    find.byType(TextFormField).first,
    'client@example.test',
  );
  await tester.enterText(find.byType(TextFormField).last, 'test-password');
  final login = find.text('Giriş yap');
  await tester.ensureVisible(login);
  await tester.tap(login);
  await tester.pumpAndSettle();

  expect(find.text('Kişisel verileriniz'), findsOneWidget);
  await tester.scrollUntilVisible(find.text('Okudum, devam et'), 200);
  await tester.tap(find.text('Okudum, devam et'));
  await tester.pumpAndSettle();
  expect(find.text('Merhaba, Örnek Danışan'), findsOneWidget);
}

void main() {
  testWidgets('Giriş, KVKK metni ve bugünkü öğünler sunucudan gelir', (
    tester,
  ) async {
    final backend = FakeBackend();
    await signIn(tester, backend);

    expect(backend.requests.take(2), [
      'POST /api/auth/login',
      'GET /api/me/profile',
    ]);
    expect(backend.requests, contains('GET /api/me/meals/today'));
    expect(find.text('Kahvaltı'), findsOneWidget);
    expect(find.text('08.00 · Planlı'), findsOneWidget);

    await tester.tap(find.text('Diyet listem'));
    await tester.pumpAndSettle();
    expect(find.text('Deneme planı'), findsOneWidget);
    expect(find.text('Yulaf ezmesi'), findsOneWidget);
    expect(find.text('4 × 1 yemek kaşığı · 40 g'), findsOneWidget);
  });

  testWidgets('Danışan ölçüm geçmişini görür; 401 gelince girişe döner', (
    tester,
  ) async {
    final backend = FakeBackend();
    await signIn(tester, backend);
    await tester.tap(find.text('Gelişim'));
    await tester.pumpAndSettle();

    expect(find.text('71,65 kg'), findsOneWidget);
    expect(find.text('Hedefe fark 3,65 kg'), findsOneWidget);
    expect(find.textContaining('Kontrol ölçümü'), findsOneWidget);
    await tester.tap(find.text('Bel çevresi'));
    await tester.pumpAndSettle();
    expect(find.text('83 cm'), findsOneWidget);

    backend.measurementStatus = 503;
    await tester.tap(find.byTooltip('Ölçümleri yenile'));
    await tester.pumpAndSettle();
    expect(find.text('Tekrar dene'), findsOneWidget);

    backend
      ..measurementStatus = 200
      ..weight = 71;
    await tester.tap(find.text('Tekrar dene'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Kilo'));
    await tester.pumpAndSettle();
    expect(find.text('71 kg'), findsOneWidget);
    expect(find.text('Hedefe fark 3 kg'), findsOneWidget);

    backend.measurementStatus = 401;
    await tester.tap(find.byTooltip('Ölçümleri yenile'));
    await tester.pumpAndSettle();
    expect(find.text('Danışan girişi'), findsOneWidget);
    expect(
      find.text('Oturumunuz sona erdi. Yeniden giriş yapın.'),
      findsOneWidget,
    );
  });

  testWidgets('Öğün "Yedim" olarak kaydedilir ve listede görünür', (
    tester,
  ) async {
    final backend = FakeBackend();
    await signIn(tester, backend);

    await tester.tap(find.text('Kahvaltı'));
    await tester.pumpAndSettle();
    expect(find.text('Öğün kaydı'), findsOneWidget);
    await tester.tap(find.text('Yedim'));
    await tester.pumpAndSettle();

    final save = find.text('Öğünü kaydet');
    await tester.scrollUntilVisible(
      save,
      150,
      scrollable: find
          .descendant(
            of: find.byType(MealEditor),
            matching: find.byType(Scrollable),
          )
          .first,
    );
    // scrollUntilVisible buton oluşunca durur; tam ekrana girmesi için ayrıca kaydır.
    await tester.ensureVisible(save);
    await tester.pumpAndSettle();
    await tester.tap(save);
    await tester.pumpAndSettle();

    expect(backend.bodies['POST /api/me/meal-logs'], {
      'mealId': 'meal-1',
      'logDate': FakeBackend.today,
      'status': 'Eaten',
      'note': null,
    });
    expect(find.text('08.00 · Yedim'), findsOneWidget);
    expect(find.text('1 / 1 kaydedildi'), findsOneWidget);
  });

  testWidgets('Dolu saatte 409 mesajı görünür, sonra talep gönderilir', (
    tester,
  ) async {
    final backend = FakeBackend()..slotTaken = true;
    await signIn(tester, backend);
    await tester.tap(find.text('Randevular'));
    await tester.pumpAndSettle();

    // 2026-11-18T07:00:00Z = Türkiye saatiyle 10:00
    await tester.scrollUntilVisible(find.text('10:00'), 150);
    await tester.tap(find.text('10:00'));
    await tester.pumpAndSettle();
    await tester.ensureVisible(find.text('Randevu talep et'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Randevu talep et'));
    await tester.pumpAndSettle();
    expect(
      find.text('Bu saat az önce doldu. Lütfen başka bir saat seç.'),
      findsOneWidget,
    );

    await tester.scrollUntilVisible(find.text('10:00'), 150);
    await tester.tap(find.text('10:00'));
    await tester.pumpAndSettle();
    await tester.ensureVisible(find.text('Randevu talep et'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Randevu talep et'));
    await tester.pumpAndSettle();
    expect(backend.bodies['POST /api/me/appointments'], {
      'startsAt': '2026-11-18T07:00:00.000Z',
    });
    expect(find.text('Talep edildi'), findsOneWidget);
  });

  testWidgets('Bildirimler listelenir ve izinler sunucuya kaydedilir', (
    tester,
  ) async {
    final backend = FakeBackend();
    await signIn(tester, backend);

    await tester.tap(find.byTooltip('Bildirimler, 1 okunmamış'));
    await tester.pumpAndSettle();
    expect(find.text('Yeni planın hazır'), findsOneWidget);

    await tester.scrollUntilVisible(find.text('Ölçüm'), 150);
    await tester.tap(find.text('Ölçüm'));
    await tester.pumpAndSettle();
    expect(backend.bodies['PUT /api/me/notification-settings'], {
      'appointments': true,
      'mealReminders': true,
      'measurements': false,
      'plans': true,
    });
  });

  testWidgets('Bildirime dokununca okundu olur ve ilgili sekme açılır', (
    tester,
  ) async {
    final backend = FakeBackend();
    await signIn(tester, backend);
    await tester.tap(find.text('Randevular'));
    await tester.pumpAndSettle();

    await tester.tap(find.byTooltip('Bildirimler, 1 okunmamış'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Yeni planın hazır'));
    await tester.pumpAndSettle();

    expect(backend.requests, contains('POST /api/notifications/n-1/read'));
    // PlanPublished → Bugün sekmesi
    expect(find.text('Bugünün öğünleri'), findsOneWidget);
  });

  testWidgets('Şifremi unuttum bağlantısı e-postaya gönderilir', (
    tester,
  ) async {
    final backend = FakeBackend();
    await tester.pumpWidget(DanisanTakipApp(api: backend.api));
    await tester.enterText(
      find.byType(TextFormField).first,
      'client@example.test',
    );
    final forgot = find.text('Şifremi unuttum');
    await tester.ensureVisible(forgot);
    await tester.tap(forgot);
    await tester.pumpAndSettle();

    expect(backend.bodies['POST /api/auth/forgot-password'], {
      'email': 'client@example.test',
    });
    expect(
      find.textContaining('sıfırlama bağlantısı gönderildi'),
      findsOneWidget,
    );
  });
}
