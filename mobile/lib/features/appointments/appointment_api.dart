import '../../core/api_client.dart';
import '../../core/format.dart';
import '../../core/json.dart';

/// Randevu durumları (BR-09) ve ekrandaki adları.
const appointmentStatusLabels = {
  'Requested': 'Talep edildi',
  'Confirmed': 'Onaylandı',
  'Cancelled': 'İptal edildi',
  'Completed': 'Tamamlandı',
};

class Appointment {
  const Appointment({
    required this.id,
    required this.startsAt,
    required this.endsAt,
    required this.status,
  });

  final String id;

  /// UTC; ekranda [turkeyTime] ile gösterilir.
  final DateTime startsAt;
  final DateTime endsAt;
  final String status;

  bool get isActive => status == 'Requested' || status == 'Confirmed';
  String get statusLabel => appointmentStatusLabels[status] ?? status;

  factory Appointment.fromJson(Object? json) {
    final map = asMap(json);
    return Appointment(
      id: readString(map, 'id'),
      startsAt: readDateTime(map, 'startsAt'),
      endsAt: readDateTime(map, 'endsAt'),
      status: readString(map, 'status'),
    );
  }
}

typedef Slot = ({DateTime startsAt, DateTime endsAt});

/// Danışanın randevu uçları: /api/me/... (sd-7).
class AppointmentApi {
  const AppointmentApi(this.session);

  final SessionApi session;

  Future<List<Appointment>> list() async => readResponse(
    await session.get('/api/me/appointments'),
    (json) => [for (final row in asList(json)) Appointment.fromJson(row)],
  );

  /// Diyetisyenin o günkü boş 30 dakikalık slotları (UTC).
  Future<List<Slot>> slots(DateTime day) async => readResponse(
    await session.get('/api/me/available-slots?date=${dateKey(day)}'),
    (json) => [
      for (final row in asList(json))
        (
          startsAt: readDateTime(asMap(row), 'startsAt'),
          endsAt: readDateTime(asMap(row), 'endsAt'),
        ),
    ],
  );

  /// Slot az önce dolduysa 409 "Bu saat az önce doldu".
  Future<void> request(DateTime startsAt) => session.post(
    '/api/me/appointments',
    {'startsAt': startsAt.toUtc().toIso8601String()},
  );

  /// Danışan ertelerse randevu yeniden "Talep edildi" olur.
  Future<void> reschedule(String id, DateTime startsAt) => session.post(
    '/api/me/appointments/$id/reschedule',
    {'startsAt': startsAt.toUtc().toIso8601String()},
  );

  Future<void> cancel(String id) =>
      session.post('/api/me/appointments/$id/cancel');
}
