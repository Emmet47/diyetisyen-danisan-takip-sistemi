import 'package:ddts_mobile/main.dart';
import 'package:flutter_test/flutter_test.dart';

void main() {
  testWidgets('API durumu ekranda gösteriliyor', (tester) async {
    // Gerçek ağ çağrısı yerine sahte bir sağlık kontrolü veriyoruz.
    await tester.pumpWidget(DdtsApp(healthCheck: () async => 'Healthy'));
    await tester.pumpAndSettle();

    expect(find.text('API durumu: Healthy'), findsOneWidget);
  });
}
