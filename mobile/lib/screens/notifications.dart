import 'package:material_ui/material_ui.dart';

import '../core/api_client.dart';
import '../core/format.dart';
import '../features/notifications/notification_api.dart';
import '../ui.dart';

/// Bildirim listesi ve bildirim izinleri (UC-31, FR-J2, FR-J3).
class NotificationsScreen extends StatefulWidget {
  const NotificationsScreen({super.key, required this.api});

  final NotificationApi api;

  @override
  State<NotificationsScreen> createState() => _NotificationsScreenState();
}

class _NotificationsScreenState extends State<NotificationsScreen> {
  List<AppNotification>? items;
  NotificationSettings? settings;
  bool saving = false;
  String? error;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    try {
      final list = await widget.api.list();
      final current = await widget.api.settings();
      if (!mounted) return;
      setState(() {
        error = null;
        items = list;
        settings = current;
      });
    } on ApiException catch (failure) {
      if (mounted) setState(() => error = failure.message);
    }
  }

  Future<void> _markRead(AppNotification item) async {
    if (item.isRead) return;
    try {
      await widget.api.markRead(item.id);
      await _load();
    } on ApiException catch (failure) {
      if (mounted) setState(() => error = failure.message);
    }
  }

  Future<void> _change(NotificationSettings next) async {
    final previous = settings;
    setState(() {
      saving = true;
      settings = next;
      error = null;
    });
    try {
      await widget.api.saveSettings(next);
    } on ApiException catch (failure) {
      if (mounted) {
        setState(() {
          settings = previous;
          error = failure.message;
        });
      }
    } finally {
      if (mounted) setState(() => saving = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final list = items;
    final current = settings;
    final message = error;
    return Scaffold(
      appBar: AppBar(title: const Text('Bildirimler')),
      body: SafeArea(
        child: RefreshIndicator(
          onRefresh: _load,
          child: ListView(
            padding: const EdgeInsets.all(22),
            children: [
              if (message != null)
                Padding(
                  padding: const EdgeInsets.only(bottom: 12),
                  child: Text(
                    message,
                    style: TextStyle(
                      color: Theme.of(context).colorScheme.error,
                    ),
                  ),
                ),
              if (list == null && message == null)
                const Center(child: CircularProgressIndicator())
              else if (list != null && list.isEmpty)
                const Text('Henüz bildirimin yok.')
              else if (list != null)
                for (final item in list)
                  ListTile(
                    contentPadding: EdgeInsets.zero,
                    leading: Icon(
                      item.isRead
                          ? Icons.drafts_outlined
                          : Icons.mark_email_unread_outlined,
                      color: teal,
                    ),
                    title: Text(item.title),
                    subtitle: Text(
                      '${item.body}\n'
                      '${dayLabel(turkeyTime(item.createdAt))} ${timeLabel(turkeyTime(item.createdAt))} · '
                      '${item.isRead ? 'Okundu' : 'Okunmadı · Okundu olarak işaretle'}',
                    ),
                    isThreeLine: true,
                    onTap: () => _markRead(item),
                  ),
              const Divider(),
              Text(
                'Hatırlatma tercihleri',
                style: Theme.of(context).textTheme.titleLarge,
              ),
              const Text(
                'Kapattığın türde bildirim gönderilmez.',
                style: TextStyle(color: muted),
              ),
              if (current != null) ...[
                SwitchListTile(
                  contentPadding: EdgeInsets.zero,
                  title: const Text('Randevu'),
                  value: current.appointments,
                  onChanged: saving
                      ? null
                      : (value) => _change(
                          NotificationSettings(
                            appointments: value,
                            mealReminders: current.mealReminders,
                            measurements: current.measurements,
                            plans: current.plans,
                          ),
                        ),
                ),
                SwitchListTile(
                  contentPadding: EdgeInsets.zero,
                  title: const Text('Öğün'),
                  value: current.mealReminders,
                  onChanged: saving
                      ? null
                      : (value) => _change(
                          NotificationSettings(
                            appointments: current.appointments,
                            mealReminders: value,
                            measurements: current.measurements,
                            plans: current.plans,
                          ),
                        ),
                ),
                SwitchListTile(
                  contentPadding: EdgeInsets.zero,
                  title: const Text('Ölçüm'),
                  value: current.measurements,
                  onChanged: saving
                      ? null
                      : (value) => _change(
                          NotificationSettings(
                            appointments: current.appointments,
                            mealReminders: current.mealReminders,
                            measurements: value,
                            plans: current.plans,
                          ),
                        ),
                ),
                SwitchListTile(
                  contentPadding: EdgeInsets.zero,
                  title: const Text('Yeni plan'),
                  value: current.plans,
                  onChanged: saving
                      ? null
                      : (value) => _change(
                          NotificationSettings(
                            appointments: current.appointments,
                            mealReminders: current.mealReminders,
                            measurements: current.measurements,
                            plans: value,
                          ),
                        ),
                ),
              ],
            ],
          ),
        ),
      ),
    );
  }
}
