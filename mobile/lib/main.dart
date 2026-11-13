import 'package:dio/dio.dart';
import 'package:material_ui/material_ui.dart';

import 'core/api_client.dart';

void main() {
  runApp(const DdtsApp());
}

/// API'nin ayakta olup olmadığını sorar. Başlangıç ekranında bağlantıyı göstermek için.
Future<String> fetchApiHealth() async {
  try {
    final response = await apiClient.get<String>(
      '/api/health',
      options: Options(responseType: ResponseType.plain),
    );
    return response.data ?? '';
  } on DioException catch (e) {
    final statusCode = e.response?.statusCode;
    return statusCode == null ? "API'ye ulaşılamadı" : 'Hata ($statusCode)';
  }
}

class DdtsApp extends StatelessWidget {
  const DdtsApp({super.key, this.healthCheck = fetchApiHealth});

  /// Testlerde gerçek ağ çağrısı yerine sahte fonksiyon verebilmek için dışarıdan alınıyor.
  final Future<String> Function() healthCheck;

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'DDTS Danışan',
      theme: ThemeData(colorSchemeSeed: Colors.green),
      home: HomePage(healthCheck: healthCheck),
    );
  }
}

/// Başlangıç ekranı. Gerçek ekranlar (giriş, bugünkü öğünler, öğün kaydı, randevular)
/// lib/features altına modül modül eklenecek.
class HomePage extends StatefulWidget {
  const HomePage({super.key, required this.healthCheck});

  final Future<String> Function() healthCheck;

  @override
  State<HomePage> createState() => _HomePageState();
}

class _HomePageState extends State<HomePage> {
  late final Future<String> _health = widget.healthCheck();

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Diyetisyen Danışan Takip')),
      body: Center(
        child: FutureBuilder<String>(
          future: _health,
          builder: (context, snapshot) {
            final durum = snapshot.data ?? 'kontrol ediliyor...';
            return Text('API durumu: $durum');
          },
        ),
      ),
    );
  }
}
