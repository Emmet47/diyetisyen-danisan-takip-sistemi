import 'package:material_ui/material_ui.dart';

import '../core/api_client.dart';
import '../core/format.dart';
import '../features/appointments/appointment_api.dart';
import '../ui.dart';

/// Randevu talep etme, erteleme ve iptal (UC-29, UC-30, sd-7).
/// Saatler Türkiye saatiyle gösterilir; API'ye UTC gönderilir.
class AppointmentsScreen extends StatefulWidget {
  const AppointmentsScreen({super.key, required this.api});

  final AppointmentApi api;

  @override
  State<AppointmentsScreen> createState() => _AppointmentsScreenState();
}

class _AppointmentsScreenState extends State<AppointmentsScreen> {
  List<Appointment>? appointments;
  List<Slot>? slots;
  late DateTime day;
  DateTime? selected;
  Appointment? editing;
  bool busy = false;
  String? error;
  String? info;

  @override
  void initState() {
    super.initState();
    day = turkeyToday().add(const Duration(days: 1));
    _loadAll();
  }

  Future<void> _loadAll() async {
    await _loadAppointments();
    await _loadSlots();
  }

  Future<void> _loadAppointments() async {
    try {
      final list = await widget.api.list();
      if (!mounted) return;
      setState(() => appointments = list);
    } on ApiException catch (failure) {
      if (mounted) setState(() => error = failure.message);
    }
  }

  Future<void> _loadSlots() async {
    if (!mounted) return;
    setState(() {
      slots = null;
      selected = null;
    });
    try {
      final list = await widget.api.slots(day);
      if (mounted) setState(() => slots = list);
    } on ApiException catch (failure) {
      if (mounted) {
        setState(() {
          slots = const [];
          error = failure.message;
        });
      }
    }
  }

  Future<void> chooseDate() async {
    final today = turkeyToday();
    final first = DateTime(today.year, today.month, today.day);
    final last = DateTime(today.year, today.month, today.day + 60);
    final initial = DateTime(day.year, day.month, day.day);
    final chosen = await showDatePicker(
      context: context,
      initialDate: initial.isBefore(first)
          ? first
          : initial.isAfter(last)
          ? last
          : initial,
      firstDate: first,
      lastDate: last,
      helpText: 'Randevu günü',
    );
    if (chosen == null || !mounted) return;
    setState(() => day = DateTime.utc(chosen.year, chosen.month, chosen.day));
    await _loadSlots();
  }

  Future<void> _run(Future<void> Function() action, String done) async {
    setState(() {
      busy = true;
      error = null;
      info = null;
    });
    try {
      await action();
      if (!mounted) return;
      setState(() {
        info = done;
        editing = null;
      });
      await _loadAll();
    } on ApiException catch (failure) {
      if (!mounted) return;
      setState(() => error = failure.message);
      // 409 "Bu saat az önce doldu": boş saatler yeniden alınır.
      if (failure.statusCode == 409) await _loadSlots();
    } finally {
      if (mounted) setState(() => busy = false);
    }
  }

  void save() {
    final time = selected;
    if (time == null || busy) return;
    final current = editing;
    if (current == null) {
      _run(
        () => widget.api.request(time),
        'Randevu talebin gönderildi. Diyetisyenin onaylayınca bildirim alacaksın.',
      );
    } else {
      _run(
        () => widget.api.reschedule(current.id, time),
        'Randevun ertelendi; diyetisyeninin onayı bekleniyor.',
      );
    }
  }

  Future<void> cancel(Appointment appointment) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Randevu iptal edilsin mi?'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('Vazgeç'),
          ),
          FilledButton(
            onPressed: () => Navigator.pop(context, true),
            child: const Text('İptal et'),
          ),
        ],
      ),
    );
    if (confirmed != true || !mounted) return;
    await _run(
      () => widget.api.cancel(appointment.id),
      'Randevu iptal edildi.',
    );
  }

  @override
  Widget build(BuildContext context) {
    final heading = Theme.of(context).textTheme.titleMedium;
    final list = appointments;
    final ordered = [...?list]..sort((a, b) => a.startsAt.compareTo(b.startsAt));
    final available = slots;
    final message = error;
    final success = info;
    return RefreshIndicator(
      onRefresh: _loadAll,
      child: ListView(
        padding: const EdgeInsets.all(20),
        children: [
          const ScreenHeading(
            'TAKVİM',
            'Randevular',
            subtitle: 'Diyetisyeninle görüşmelerini takip et.',
          ),
          if (message != null) ...[
            const SizedBox(height: 12),
            Text(
              message,
              style: TextStyle(color: Theme.of(context).colorScheme.error),
            ),
          ],
          if (success != null) ...[
            const SizedBox(height: 12),
            Text(success, style: const TextStyle(color: teal)),
          ],
          if (list == null)
            const Padding(
              padding: EdgeInsets.all(20),
              child: Center(child: CircularProgressIndicator()),
            )
          else if (ordered.isEmpty) ...[
            const SizedBox(height: 12),
            const Panel(child: Text('Henüz randevun yok.')),
          ],
          for (final appointment in ordered) ...[
            const SizedBox(height: 12),
            _AppointmentCard(
              appointment: appointment,
              busy: busy,
              onReschedule: () {
                final start = turkeyTime(appointment.startsAt);
                setState(() {
                  editing = appointment;
                  day = DateTime.utc(start.year, start.month, start.day);
                  info = null;
                });
                _loadSlots();
              },
              onCancel: () => cancel(appointment),
            ),
          ],
          const SizedBox(height: 12),
          Panel(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  editing == null
                      ? 'Yeni randevu talep et'
                      : 'Randevuyu ertele',
                  style: heading,
                ),
                TextButton.icon(
                  onPressed: busy ? null : chooseDate,
                  icon: const Icon(Icons.calendar_today_outlined, size: 16),
                  label: Text(
                    '${dayLabel(day)} ${weekdays[day.weekday - 1]} · Uygun saatler',
                  ),
                ),
                if (available == null)
                  const Padding(
                    padding: EdgeInsets.all(8),
                    child: LinearProgressIndicator(),
                  )
                else if (available.isEmpty)
                  const Text(
                    'Bu gün için boş saat yok. Başka bir gün seç.',
                    style: TextStyle(color: muted),
                  )
                else
                  Wrap(
                    spacing: 7,
                    children: [
                      for (final slot in available)
                        ChoiceChip(
                          label: Text(timeLabel(turkeyTime(slot.startsAt))),
                          selected: selected == slot.startsAt,
                          onSelected: busy
                              ? null
                              : (active) => setState(
                                  () => selected = active ? slot.startsAt : null,
                                ),
                        ),
                    ],
                  ),
                const SizedBox(height: 8),
                Center(
                  child: FilledButton(
                    onPressed: selected == null || busy ? null : save,
                    child: Text(
                      editing == null
                          ? 'Randevu talep et'
                          : 'Ertelemeyi kaydet',
                    ),
                  ),
                ),
                if (editing != null)
                  Center(
                    child: TextButton(
                      onPressed: () => setState(() {
                        editing = null;
                        selected = null;
                      }),
                      child: const Text('Vazgeç'),
                    ),
                  ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}

class _AppointmentCard extends StatelessWidget {
  const _AppointmentCard({
    required this.appointment,
    required this.busy,
    required this.onReschedule,
    required this.onCancel,
  });

  final Appointment appointment;
  final bool busy;
  final VoidCallback onReschedule;
  final VoidCallback onCancel;

  @override
  Widget build(BuildContext context) {
    final start = turkeyTime(appointment.startsAt);
    final end = turkeyTime(appointment.endsAt);
    final upcoming = appointment.startsAt.isAfter(DateTime.now().toUtc());
    return Panel(
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Flexible(
                child: Text(
                  'Görüşme',
                  style: Theme.of(context).textTheme.titleMedium,
                ),
              ),
              Chip(
                label: Text(appointment.statusLabel),
                backgroundColor: appointment.isActive ? mint : null,
              ),
            ],
          ),
          Text(
            '${dayLabel(start)} ${weekdays[start.weekday - 1]}',
            style: const TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
          ),
          const SizedBox(height: 4),
          Text(
            '${timeLabel(start)}–${timeLabel(end)} · 30 dakika (Türkiye saati)',
            style: const TextStyle(color: muted),
          ),
          if (appointment.isActive && upcoming)
            Wrap(
              spacing: 4,
              children: [
                TextButton(
                  onPressed: busy ? null : onReschedule,
                  child: const Text('Ertele'),
                ),
                TextButton(
                  onPressed: busy ? null : onCancel,
                  child: const Text('İptal et'),
                ),
              ],
            ),
        ],
      ),
    );
  }
}
