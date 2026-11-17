import 'dart:async';

import 'package:flutter/material.dart';

import 'core/api_client.dart';
import 'demo_data.dart';
import 'features/auth/client_auth.dart';
import 'features/auth/login_screen.dart';
import 'ui.dart';
import 'screens/appointments.dart';
import 'screens/meals.dart';
import 'screens/progress.dart';

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

class _SessionGate extends StatefulWidget {
  const _SessionGate({required this.api});

  final ApiClient api;

  @override
  State<_SessionGate> createState() => _SessionGateState();
}

class _SessionGateState extends State<_SessionGate> {
  ClientSession? _session;
  Timer? _expiry;
  bool _demo = false;
  int _liveTab = 0;

  void _openSession(ClientSession session) {
    final remaining = session.expiresAt.difference(DateTime.now());
    if (remaining <= Duration.zero) return;
    _expiry?.cancel();
    _expiry = Timer(remaining, _closeSession);
    setState(() {
      _session = session;
      _liveTab = 0;
    });
  }

  void _closeSession() {
    _expiry?.cancel();
    setState(() {
      _session = null;
      _demo = false;
    });
  }

  @override
  void dispose() {
    _expiry?.cancel();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    if (_demo) {
      return PopScope(
        canPop: false,
        onPopInvokedWithResult: (didPop, _) {
          if (!didPop) _closeSession();
        },
        child: const _ClientHome(),
      );
    }
    if (_session case final session?) {
      return Scaffold(
        appBar: AppBar(
          title: const Text('Danışan Takip'),
          actions: [
            IconButton(
              tooltip: 'Çıkış yap',
              onPressed: _closeSession,
              icon: const Icon(Icons.logout),
            ),
          ],
        ),
        body: SafeArea(
          bottom: false,
          child: _liveTab == 0
              ? ListView(
                  padding: const EdgeInsets.all(20),
                  children: [
                    ScreenHeading('Danışan', 'Merhaba, ${session.fullName}'),
                    const SizedBox(height: 18),
                    const Panel(
                      child: ListTile(
                        contentPadding: EdgeInsets.zero,
                        leading: Icon(Icons.restaurant_outlined, color: teal),
                        title: Text('Bugünün öğünleri'),
                        subtitle: Text('Öğün verileri şu anda kullanılamıyor.'),
                      ),
                    ),
                  ],
                )
              : ProgressScreen.live(
                  token: session.accessToken,
                  api: widget.api,
                  onUnauthorized: _closeSession,
                ),
        ),
        bottomNavigationBar: NavigationBar(
          backgroundColor: chrome,
          indicatorColor: chromeSelection,
          surfaceTintColor: Colors.transparent,
          selectedIndex: _liveTab,
          onDestinationSelected: (index) => setState(() => _liveTab = index),
          destinations: const [
            NavigationDestination(
              icon: Icon(Icons.today_outlined),
              label: 'Bugün',
            ),
            NavigationDestination(
              icon: Icon(Icons.show_chart),
              label: 'Gelişim',
            ),
          ],
        ),
      );
    }
    return LoginScreen(
      authenticate: (email, password) =>
          signInClient(email, password, api: widget.api),
      onSignedIn: _openSession,
      onDemo: () => setState(() => _demo = true),
    );
  }
}

class _ClientHome extends StatefulWidget {
  const _ClientHome();

  @override
  State<_ClientHome> createState() => _ClientHomeState();
}

class _ClientHomeState extends State<_ClientHome> {
  final entries = <Meal, MealEntry>{
    for (final meal in meals.take(3))
      meal: (
        status: MealStatus.eaten,
        note: '',
        portionPercent: 100,
        photo: null,
        foods: const [],
      ),
  };
  final preferences = {
    'Randevu': true,
    'Öğün': true,
    'Ölçüm': true,
    'Yeni plan': true,
  };
  int index = 0;
  bool notificationRead = false;

  Future<void> editMeal(Meal meal) async {
    final entry = await Navigator.push<MealEntry>(
      context,
      MaterialPageRoute(
        builder: (_) => MealEditor(meal: meal, entry: entries[meal]),
      ),
    );
    if (entry == null || !mounted) return;
    setState(() => entries[meal] = entry);
  }

  Future<void> openNotifications() async {
    await Navigator.push<void>(
      context,
      MaterialPageRoute(
        builder: (context) => StatefulBuilder(
          builder: (context, refresh) => Scaffold(
            appBar: AppBar(title: const Text('Bildirimler')),
            body: ListView(
              padding: const EdgeInsets.all(22),
              children: [
                ListTile(
                  contentPadding: EdgeInsets.zero,
                  leading: Icon(
                    notificationRead
                        ? Icons.drafts_outlined
                        : Icons.mark_email_unread_outlined,
                  ),
                  title: const Text('Yeni planın hazır'),
                  subtitle: Text(
                    notificationRead
                        ? 'Okundu'
                        : 'Okunmadı · Okundu olarak işaretle',
                  ),
                  onTap: () {
                    setState(() => notificationRead = true);
                    refresh(() {});
                  },
                ),
                const Divider(),
                Text(
                  'Hatırlatma tercihleri',
                  style: Theme.of(context).textTheme.titleLarge,
                ),
                for (final item in preferences.entries)
                  SwitchListTile(
                    contentPadding: EdgeInsets.zero,
                    title: Text(item.key),
                    value: item.value,
                    onChanged: (value) =>
                        refresh(() => preferences[item.key] = value),
                  ),
              ],
            ),
          ),
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Danışan Takip'),
        actions: [
          const Center(child: Text('Demo')),
          const SizedBox(width: 6),
          IconButton(
            tooltip: 'Bildirimler',
            onPressed: openNotifications,
            icon: Badge(
              isLabelVisible: !notificationRead,
              child: const Icon(Icons.notifications_outlined),
            ),
          ),
        ],
      ),
      body: SafeArea(
        bottom: false,
        child: IndexedStack(
          index: index,
          children: [
            TodayScreen(entries: entries, onMealTap: editMeal),
            DiaryScreen(entries: entries, onMealTap: editMeal),
            const ProgressScreen(),
            const AppointmentsScreen(),
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
          NavigationDestination(icon: Icon(Icons.show_chart), label: 'Gelişim'),
          NavigationDestination(
            icon: Icon(Icons.event_available_outlined),
            label: 'Randevular',
          ),
        ],
      ),
    );
  }
}
