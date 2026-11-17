import 'package:material_ui/material_ui.dart';

import '../demo_data.dart';
import '../ui.dart';

class AppointmentsScreen extends StatefulWidget {
  const AppointmentsScreen({super.key});

  @override
  State<AppointmentsScreen> createState() => _AppointmentsScreenState();
}

class _AppointmentsScreenState extends State<AppointmentsScreen> {
  late final DateTime firstDay;
  late final List<Appointment> appointments;
  late DateTime day;
  DateTime? selected;
  int? editingIndex;

  @override
  void initState() {
    super.initState();
    final now = DateTime.now();
    firstDay = DateTime(now.year, now.month, now.day + 1);
    day = DateTime(now.year, now.month, now.day + 3);
    appointments = [
      (
        time: DateTime(now.year, now.month, now.day + 2, 14, 30),
        status: 'Onaylandı',
      ),
    ];
  }

  Future<void> chooseDate() async {
    final chosen = await showDatePicker(
      context: context,
      initialDate: day,
      firstDate: firstDay,
      lastDate: DateTime(firstDay.year, firstDay.month, firstDay.day + 29),
      helpText: 'Randevu günü',
    );
    if (chosen == null || !mounted) return;
    setState(() {
      day = chosen;
      selected = null;
    });
  }

  bool isTaken(DateTime time) => appointments.indexed.any(
    (item) =>
        item.$1 != editingIndex &&
        item.$2.time == time &&
        item.$2.status != 'İptal edildi',
  );

  void save() {
    final time = selected;
    if (time == null || isTaken(time)) return;
    setState(() {
      final index = editingIndex;
      final appointment = (time: time, status: 'Talep edildi');
      if (index == null) {
        appointments.add(appointment);
      } else {
        appointments[index] = appointment;
      }
      editingIndex = null;
      selected = null;
    });
  }

  Future<void> cancel(int index) async {
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
    setState(() {
      appointments[index] = (
        time: appointments[index].time,
        status: 'İptal edildi',
      );
      if (editingIndex == index) {
        editingIndex = null;
        selected = null;
      }
    });
  }

  @override
  Widget build(BuildContext context) {
    final heading = Theme.of(context).textTheme.titleMedium;
    final slots = [
      for (final (hour, minute) in [(10, 0), (10, 30), (14, 0), (14, 30)])
        DateTime(day.year, day.month, day.day, hour, minute),
    ];
    final ordered = appointments.indexed.toList()
      ..sort((a, b) => a.$2.time.compareTo(b.$2.time));
    return ListView(
      padding: const EdgeInsets.all(20),
      children: [
        const ScreenHeading(
          'TAKVİM',
          'Randevular',
          subtitle: 'Diyetisyeninle görüşmelerini takip et.',
        ),
        for (final (index, appointment) in ordered) ...[
          const SizedBox(height: 12),
          Panel(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Flexible(child: Text('Görüşme', style: heading)),
                    Chip(
                      label: Text(appointment.status),
                      backgroundColor: appointment.status == 'İptal edildi'
                          ? null
                          : mint,
                    ),
                  ],
                ),
                Text(
                  '${dayLabel(appointment.time)} ${weekdays[appointment.time.weekday - 1]}',
                  style: const TextStyle(
                    fontSize: 18,
                    fontWeight: FontWeight.bold,
                  ),
                ),
                const SizedBox(height: 4),
                Text(
                  '${timeLabel(appointment.time)}–${timeLabel(appointment.time.add(const Duration(minutes: 30)))} · 30 dakika',
                  style: const TextStyle(color: muted),
                ),
                if (appointment.status != 'İptal edildi')
                  Wrap(
                    spacing: 4,
                    children: [
                      TextButton(
                        onPressed: () {
                          setState(() {
                            editingIndex = index;
                            day = DateTime(
                              appointment.time.year,
                              appointment.time.month,
                              appointment.time.day,
                            );
                            selected = null;
                          });
                        },
                        child: const Text('Ertele'),
                      ),
                      TextButton(
                        onPressed: () => cancel(index),
                        child: const Text('İptal et'),
                      ),
                    ],
                  ),
              ],
            ),
          ),
        ],
        const SizedBox(height: 12),
        Panel(
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                editingIndex == null
                    ? 'Yeni randevu talep et'
                    : 'Randevuyu ertele',
                style: heading,
              ),
              TextButton.icon(
                onPressed: chooseDate,
                icon: const Icon(Icons.calendar_today_outlined, size: 16),
                label: Text(
                  '${dayLabel(day)} ${weekdays[day.weekday - 1]} · Uygun saatler',
                ),
              ),
              Wrap(
                spacing: 7,
                children: [
                  for (final time in slots)
                    ChoiceChip(
                      label: Text(timeLabel(time)),
                      selected: selected == time,
                      onSelected: isTaken(time)
                          ? null
                          : (active) =>
                                setState(() => selected = active ? time : null),
                    ),
                ],
              ),
              const SizedBox(height: 8),
              Center(
                child: FilledButton(
                  onPressed: selected == null ? null : save,
                  child: Text(
                    editingIndex == null
                        ? 'Randevu talep et'
                        : 'Ertelemeyi kaydet',
                  ),
                ),
              ),
              if (editingIndex != null)
                Center(
                  child: TextButton(
                    onPressed: () => setState(() {
                      editingIndex = null;
                      selected = null;
                    }),
                    child: const Text('Vazgeç'),
                  ),
                ),
            ],
          ),
        ),
      ],
    );
  }
}
