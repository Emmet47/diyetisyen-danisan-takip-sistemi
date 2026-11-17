import 'dart:async';

import 'package:material_ui/material_ui.dart';

import 'core/api_client.dart';
import 'features/appointments/appointment_api.dart';
import 'features/auth/client_auth.dart';
import 'features/auth/kvkk_notice.dart';
import 'features/auth/login_screen.dart';
import 'features/meals/meal_models.dart';
import 'features/notifications/notification_api.dart';
import 'screens/appointments.dart';
import 'screens/meals.dart';
import 'screens/notifications.dart';
import 'screens/progress.dart';
import 'ui.dart';

void main() => runApp(const DanisanTakipApp());

class DanisanTakipApp extends StatelessWidget {
  const DanisanTakipApp({super.key, this.api = const ApiClient()});

  final ApiClient api;

  @override
  Widget build(BuildContext context) {
    final colors = ColorScheme.fromSeed(
      seedColor: teal,
      primary: teal,
      onSurface: ink,
    );
    return MaterialApp(
      title: 'Diyetisyen Danışan Takip',
      debugShowCheckedModeBanner: false,
      theme: ThemeData(
        colorScheme: colors,
        scaffoldBackgroundColor: canvas,
        appBarTheme: const AppBarTheme(
          backgroundColor: chrome,
          surfaceTintColor: Colors.transparent,
        ),
      ),
      home: _SessionGate(api: api),
    );
  }
}

/// Oturum kapısı: giriş → KVKK aydınlatma metni → danışan ana ekranı.
/// JWT 60 dakika geçerli ve yenileme anahtarı yok: süre dolunca ya da API 401
/// dönünce giriş ekranına dönülür.
class _SessionGate extends StatefulWidget {
  const _SessionGate({required this.api});

  final ApiClient api;

  @override
  State<_SessionGate> createState() => _SessionGateState();
}

class _SessionGateState extends State<_SessionGate> {
  ClientSession? _session;
  bool _noticeAccepted = false;
  bool _expired = false;
  Timer? _expiry;

  void _openSession(ClientSession session) {
    final remaining = session.expiresAt.difference(DateTime.now());
    if (remaining <= Duration.zero) return;
    _expiry?.cancel();
    _expiry = Timer(remaining, () => _closeSession(expired: true));
    setState(() {
      _session = session;
      _noticeAccepted = false;
      _expired = false;
    });
  }

  void _closeSession({bool expired = false}) {
    _expiry?.cancel();
    if (!mounted) return;
    setState(() {
      _session = null;
      _expired = expired;
    });
  }

  @override
  void dispose() {
    _expiry?.cancel();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final session = _session;
    if (session == null) {
      return LoginScreen(
        key: ValueKey(_expired),
        authenticate: (email, password) =>
            signInClient(email, password, api: widget.api),
        forgotPassword: (email) => requestPasswordReset(email, api: widget.api),
        onSignedIn: _openSession,
        expiredNotice: _expired,
      );
    }
    if (!_noticeAccepted) {
      return KvkkNoticeScreen(
        onAccepted: () => setState(() => _noticeAccepted = true),
        onCancel: _closeSession,
      );
    }
    return ClientHome(
      key: ValueKey(session.accessToken),
      session: session,
      api: SessionApi(
        api: widget.api,
        token: session.accessToken,
        onUnauthorized: () => _closeSession(expired: true),
      ),
      onLogout: _closeSession,
    );
  }
}

/// Danışan ana ekranı: Bugün, Günlük, Gelişim, Randevular + bildirimler.
class ClientHome extends StatefulWidget {
  const ClientHome({
    super.key,
    required this.session,
    required this.api,
    required this.onLogout,
  });

  final ClientSession session;
  final SessionApi api;
  final VoidCallback onLogout;

  @override
  State<ClientHome> createState() => _ClientHomeState();
}

class _ClientHomeState extends State<ClientHome> {
  late final MealsApi meals = MealsApi(widget.api);
  late final AppointmentApi appointments = AppointmentApi(widget.api);
  late final NotificationApi notifications = NotificationApi(widget.api);
  TodayMeals? today;
  String? error;
  int unread = 0;
  int index = 0;
  int revision = 0;

  @override
  void initState() {
    super.initState();
    _loadToday();
    _loadUnread();
  }

  Future<void> _loadToday() async {
    try {
      final result = await meals.today();
      if (!mounted) return;
      setState(() {
        error = null;
        today = result;
        revision++;
      });
    } on ApiException catch (failure) {
      if (mounted) setState(() => error = failure.message);
    }
  }

  Future<void> _loadUnread() async {
    try {
      final list = await notifications.list();
      if (mounted) {
        setState(() => unread = list.where((item) => !item.isRead).length);
      }
    } on ApiException {
      // Sayaç yüklenemezse rozet gösterilmez; liste ekranı hatayı gösterir.
    }
  }

  Future<void> editMeal(TodayMeal row) async {
    final date = today?.date;
    if (date == null) return;
    final saved = await Navigator.push<bool>(
      context,
      MaterialPageRoute(
        builder: (_) => MealEditor(api: meals, date: date, row: row),
      ),
    );
    if (saved == true) await _loadToday();
  }

  Future<void> openPlan() => Navigator.push<void>(
    context,
    MaterialPageRoute(builder: (_) => PlanScreen(api: meals)),
  );

  Future<void> openNotifications() async {
    await Navigator.push<void>(
      context,
      MaterialPageRoute(
        builder: (_) => NotificationsScreen(api: notifications),
      ),
    );
    await _loadUnread();
  }

  @override
  Widget build(BuildContext context) {
    final current = today;
    final failure = error;
    final Widget mealsPage;
    final Widget diaryPage;
    if (current == null) {
      mealsPage = _Loading(error: failure, retry: _loadToday);
      diaryPage = _Loading(error: failure, retry: _loadToday);
    } else {
      mealsPage = TodayScreen(
        today: current,
        onMealTap: editMeal,
        onOpenPlan: openPlan,
        onRefresh: _loadToday,
      );
      diaryPage = DiaryScreen(
        api: meals,
        today: current,
        onMealTap: editMeal,
        revision: revision,
      );
    }
    return Scaffold(
        appBar: AppBar(
          title: Text('Merhaba, ${widget.session.fullName}'),
          actions: [
            IconButton(
              tooltip: unread > 0
                  ? 'Bildirimler, $unread okunmamış'
                  : 'Bildirimler',
              onPressed: openNotifications,
              icon: Badge(
                isLabelVisible: unread > 0,
                label: Text('$unread'),
                child: const Icon(Icons.notifications_outlined),
              ),
            ),
            IconButton(
              tooltip: 'Çıkış yap',
              onPressed: widget.onLogout,
              icon: const Icon(Icons.logout),
            ),
          ],
        ),
        body: SafeArea(
          bottom: false,
          child: IndexedStack(
            index: index,
            children: [
              mealsPage,
              diaryPage,
              ProgressScreen(api: widget.api),
              AppointmentsScreen(api: appointments),
            ],
          ),
        ),
        bottomNavigationBar: NavigationBar(
          backgroundColor: chrome,
          indicatorColor: chromeSelection,
          surfaceTintColor: Colors.transparent,
          selectedIndex: index,
          onDestinationSelected: (value) => setState(() => index = value),
          destinations: const [
            NavigationDestination(
              icon: Icon(Icons.today_outlined),
              label: 'Bugün',
            ),
            NavigationDestination(
              icon: Icon(Icons.menu_book_outlined),
              label: 'Günlük',
            ),
            NavigationDestination(
              icon: Icon(Icons.show_chart),
              label: 'Gelişim',
            ),
            NavigationDestination(
              icon: Icon(Icons.event_available_outlined),
              label: 'Randevular',
            ),
          ],
        ),
    );
  }
}

class _Loading extends StatelessWidget {
  const _Loading({required this.error, required this.retry});

  final String? error;
  final Future<void> Function() retry;

  @override
  Widget build(BuildContext context) {
    final message = error;
    if (message == null) {
      return const Center(child: CircularProgressIndicator());
    }
    return ListView(
      padding: const EdgeInsets.all(20),
      children: [
        Panel(
          child: Column(
            children: [
              Text(message),
              TextButton(onPressed: retry, child: const Text('Tekrar dene')),
            ],
          ),
        ),
      ],
    );
  }
}
