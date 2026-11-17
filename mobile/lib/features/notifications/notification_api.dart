import '../../core/api_client.dart';
import '../../core/json.dart';

class AppNotification {
  const AppNotification({
    required this.id,
    required this.title,
    required this.body,
    required this.isRead,
    required this.createdAt,
  });

  final String id;
  final String title;
  final String body;
  final bool isRead;
  final DateTime createdAt;

  factory AppNotification.fromJson(Object? json) {
    final map = asMap(json);
    return AppNotification(
      id: readString(map, 'id'),
      title: readString(map, 'title'),
      body: readString(map, 'body'),
      isRead: readBool(map, 'isRead'),
      createdAt: readDateTime(map, 'createdAt'),
    );
  }
}

/// Dört bildirim izni (FR-J2). Kapalı türde bildirim üretilmez.
class NotificationSettings {
  const NotificationSettings({
    required this.appointments,
    required this.mealReminders,
    required this.measurements,
    required this.plans,
  });

  final bool appointments;
  final bool mealReminders;
  final bool measurements;
  final bool plans;

  factory NotificationSettings.fromJson(Object? json) {
    final map = asMap(json);
    return NotificationSettings(
      appointments: readBool(map, 'appointments'),
      mealReminders: readBool(map, 'mealReminders'),
      measurements: readBool(map, 'measurements'),
      plans: readBool(map, 'plans'),
    );
  }

  Map<String, bool> toJson() => {
    'appointments': appointments,
    'mealReminders': mealReminders,
    'measurements': measurements,
    'plans': plans,
  };
}

/// Bildirim uçları: /api/notifications ve /api/me/notification-settings (sd-4).
class NotificationApi {
  const NotificationApi(this.session);

  final SessionApi session;

  /// En yenisi önce, ilk 50 bildirim.
  Future<List<AppNotification>> list() async => readResponse(
    await session.get('/api/notifications?page=1&pageSize=50'),
    (json) => [
      for (final row in asList(asMap(json)['items']))
        AppNotification.fromJson(row),
    ],
  );

  Future<void> markRead(String id) =>
      session.post('/api/notifications/$id/read');

  Future<NotificationSettings> settings() async => readResponse(
    await session.get('/api/me/notification-settings'),
    NotificationSettings.fromJson,
  );

  Future<void> saveSettings(NotificationSettings settings) =>
      session.put('/api/me/notification-settings', settings.toJson());
}
