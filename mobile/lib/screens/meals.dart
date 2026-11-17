import 'dart:math' as math;
import 'dart:typed_data';
import 'dart:ui' as ui;

import 'package:file_selector/file_selector.dart';
import 'package:image_picker/image_picker.dart';
import 'package:material_ui/material_ui.dart';

import '../core/api_client.dart';
import '../core/format.dart';
import '../core/nutrition.dart';
import '../features/meals/meal_models.dart';
import '../ui.dart';

/// Kaydedilen öğünün tahmini payı: Yedim = tamamı, Kısmen = yarısı (FR-K3'teki
/// plan uyumu tanımıyla aynı ağırlık), Yemedim = hiç.
double _share(MealStatus? status) => switch (status) {
  MealStatus.eaten => 1,
  MealStatus.partial => 0.5,
  _ => 0,
};

Nutrition _sum(Iterable<Nutrition> values) =>
    values.fold(const Nutrition(0, 0, 0), (total, item) => total + item);

/// Bugünün planlı öğünleri ve kayıtları (UC-25, FR-E5).
class TodayScreen extends StatelessWidget {
  const TodayScreen({
    super.key,
    required this.today,
    required this.onMealTap,
    required this.onOpenPlan,
    required this.onRefresh,
  });

  final TodayMeals today;
  final ValueChanged<TodayMeal> onMealTap;
  final VoidCallback onOpenPlan;
  final Future<void> Function() onRefresh;

  @override
  Widget build(BuildContext context) {
    final date = today.date;
    final target = _sum(today.meals.map((row) => row.meal.totals));
    final total = _sum(
      today.meals.map(
        (row) => row.meal.totals.portion(_share(row.log?.status) * 100),
      ),
    );
    final remaining = target.kcal - total.kcal;
    final ratio = target.kcal == 0 ? 0.0 : total.kcal / target.kcal;
    final logged = today.meals.where((row) => row.log != null).length;
    final heading = Theme.of(context).textTheme.titleMedium;
    return RefreshIndicator(
      onRefresh: onRefresh,
      child: ListView(
        padding: const EdgeInsets.all(20),
        children: [
          ScreenHeading(
            '${dayLabel(date)} ${weekdays[date.weekday - 1]}',
            'Beslenme günlüğü',
            subtitle: today.planTitle,
          ),
          const SizedBox(height: 8),
          Align(
            alignment: Alignment.centerLeft,
            child: OutlinedButton.icon(
              onPressed: onOpenPlan,
              icon: const Icon(Icons.list_alt_outlined),
              label: const Text('Diyet listem'),
            ),
          ),
          const SizedBox(height: 12),
          if (!today.hasPlan)
            const Panel(
              child: ListTile(
                contentPadding: EdgeInsets.zero,
                leading: Icon(Icons.restaurant_outlined, color: teal),
                title: Text('Henüz yayımlanmış bir planın yok'),
                subtitle: Text(
                  'Diyetisyenin planını yayımlayınca öğünlerin burada görünür.',
                ),
              ),
            )
          else ...[
            Panel(
              radius: 18,
              child: Column(
                children: [
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Text('Günlük enerji', style: heading),
                      const Flexible(
                        child: Text(
                          'DİYETİSYEN PLANI',
                          textAlign: TextAlign.end,
                          style: TextStyle(
                            color: teal,
                            fontSize: 10,
                            fontWeight: FontWeight.bold,
                          ),
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 8),
                  Row(
                    children: [
                      Expanded(
                        child: _EnergyValue(energyLabel(total.kcal), 'Alınan'),
                      ),
                      Expanded(
                        flex: 2,
                        child: SizedBox(
                          height: 118,
                          child: CustomPaint(
                            painter: _EnergyArc(ratio.clamp(0.0, 1.0)),
                            child: Center(
                              child: Padding(
                                padding: const EdgeInsets.only(top: 18),
                                child: _EnergyValue(
                                  energyLabel(remaining.abs()),
                                  remaining < 0
                                      ? 'kcal plan üstü'
                                      : 'kcal kaldı',
                                  large: true,
                                ),
                              ),
                            ),
                          ),
                        ),
                      ),
                      Expanded(
                        child: _EnergyValue(energyLabel(target.kcal), 'Plan'),
                      ),
                    ],
                  ),
                  const SizedBox(height: 6),
                  Text(
                    '${energyLabel(total.kcal)} kcal alındı · Planın %${(ratio * 100).round()}’i',
                    textAlign: TextAlign.center,
                    style: const TextStyle(color: muted, fontSize: 12),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 13),
            Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Flexible(child: Text('Bugünün öğünleri', style: heading)),
                Text(
                  '$logged / ${today.meals.length} kaydedildi',
                  style: const TextStyle(color: teal, fontSize: 12),
                ),
              ],
            ),
            const SizedBox(height: 8),
            for (final (index, row) in today.meals.indexed) ...[
              if (index > 0) const SizedBox(height: 8),
              _MealTile(row: row, index: index, onTap: () => onMealTap(row)),
            ],
            const SizedBox(height: 12),
            Panel(
              child: Column(
                children: [
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Flexible(child: Text('Makro dağılımı', style: heading)),
                      const Text(
                        'Alınan / plan',
                        style: TextStyle(color: muted, fontSize: 11),
                      ),
                    ],
                  ),
                  const SizedBox(height: 12),
                  Row(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Expanded(
                        child: _Macro(
                          'Karbonhidrat',
                          total.carbs,
                          target.carbs,
                          const Color(0xff1eaeb1),
                        ),
                      ),
                      const SizedBox(width: 10),
                      Expanded(
                        child: _Macro(
                          'Protein',
                          total.protein,
                          target.protein,
                          const Color(0xffad7123),
                        ),
                      ),
                      const SizedBox(width: 10),
                      Expanded(
                        child: _Macro(
                          'Yağ',
                          total.fat,
                          target.fat,
                          const Color(0xff7667a2),
                        ),
                      ),
                    ],
                  ),
                ],
              ),
            ),
            const SizedBox(height: 12),
            const Text(
              'Alınan değerler planlı porsiyonlara göre tahminidir (Kısmen yedim = yarısı). '
              'Hesaplamalar profesyonel değerlendirme yerine geçmez.',
              style: TextStyle(color: muted, fontSize: 11),
            ),
          ],
        ],
      ),
    );
  }
}

class _EnergyValue extends StatelessWidget {
  const _EnergyValue(this.value, this.label, {this.large = false});
  final String value;
  final String label;
  final bool large;

  @override
  Widget build(BuildContext context) => Column(
    mainAxisSize: MainAxisSize.min,
    children: [
      Text(
        value,
        textAlign: TextAlign.center,
        style: TextStyle(
          fontSize: large ? 32 : 20,
          fontWeight: FontWeight.bold,
        ),
      ),
      Text(
        label,
        textAlign: TextAlign.center,
        style: const TextStyle(color: muted, fontSize: 11),
      ),
    ],
  );
}

class _EnergyArc extends CustomPainter {
  const _EnergyArc(this.ratio);
  final double ratio;

  @override
  void paint(Canvas surface, Size size) {
    final oval = Rect.fromCircle(
      center: Offset(size.width / 2, size.height * .57),
      radius: math.min(size.width / 2 - 7, 58.0).clamp(0.0, 58.0),
    );
    final stroke = Paint()
      ..style = PaintingStyle.stroke
      ..strokeWidth = 7
      ..strokeCap = StrokeCap.round
      ..color = line;
    const start = math.pi * .87;
    const span = math.pi * 1.26;
    surface.drawArc(oval, start, span, false, stroke);
    if (ratio > 0) {
      stroke.color = const Color(0xff1eaeb1);
      surface.drawArc(oval, start, span * ratio, false, stroke);
    }
  }

  @override
  bool shouldRepaint(_EnergyArc oldDelegate) => ratio != oldDelegate.ratio;
}

class _MealTile extends StatelessWidget {
  const _MealTile({required this.row, required this.index, required this.onTap});
  final TodayMeal row;
  final int index;
  final VoidCallback onTap;

  static const icons = [
    Icons.bakery_dining_outlined,
    Icons.soup_kitchen_outlined,
    Icons.eco_outlined,
    Icons.dinner_dining_outlined,
  ];
  static const backgrounds = [
    Color(0xffe3f5ef),
    Color(0xfffef0dd),
    Color(0xfff1ebf9),
    Color(0xffe3f4f6),
  ];

  @override
  Widget build(BuildContext context) {
    final log = row.log;
    return Panel(
      radius: 14,
      padding: EdgeInsets.zero,
      child: ListTile(
        onTap: onTap,
        contentPadding: const EdgeInsets.symmetric(horizontal: 12),
        leading: Container(
          width: 40,
          height: 40,
          decoration: BoxDecoration(
            color: backgrounds[index % backgrounds.length],
            borderRadius: BorderRadius.circular(12),
          ),
          child: Icon(icons[index % icons.length], size: 22, color: teal),
        ),
        title: Text(
          row.meal.name,
          style: const TextStyle(fontWeight: FontWeight.w700),
        ),
        subtitle: Text(
          '${mealTimeLabel(row.meal.time)} · ${log?.status.label ?? 'Planlı'}'
          '${log?.hasPhoto == true ? ' · Fotoğraf' : ''}',
          style: const TextStyle(fontSize: 12, color: muted),
        ),
        trailing: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            Text(
              '${energyLabel(row.meal.totals.kcal)} kcal',
              style: const TextStyle(
                fontSize: 12,
                fontWeight: FontWeight.w700,
                color: teal,
              ),
            ),
            const SizedBox(width: 6),
            Icon(
              log == null ? Icons.add_circle_outline : Icons.chevron_right,
              color: teal,
              size: 20,
              semanticLabel: log == null ? 'Kayıt ekle' : 'Kaydı düzenle',
            ),
          ],
        ),
      ),
    );
  }
}

class _Macro extends StatelessWidget {
  const _Macro(this.label, this.value, this.target, this.color);
  final String label;
  final double value;
  final double target;
  final Color color;

  @override
  Widget build(BuildContext context) => Column(
    crossAxisAlignment: CrossAxisAlignment.start,
    children: [
      Text(
        label,
        style: const TextStyle(fontSize: 12, fontWeight: FontWeight.w500),
      ),
      const SizedBox(height: 4),
      Text(
        '${energyLabel(value)} / ${energyLabel(target)} g',
        style: const TextStyle(fontSize: 12, fontWeight: FontWeight.bold),
      ),
      const SizedBox(height: 6),
      LinearProgressIndicator(
        value: target == 0 ? 0.0 : (value / target).clamp(0.0, 1.0),
        color: color,
        backgroundColor: line,
        minHeight: 5,
        borderRadius: BorderRadius.circular(3),
        semanticsLabel: label,
      ),
    ],
  );
}

/// Öğün günlüğü: bugünkü kayıtlar ve son 7 günün kayıtları (UC-26).
class DiaryScreen extends StatefulWidget {
  const DiaryScreen({
    super.key,
    required this.api,
    required this.today,
    required this.onMealTap,
    required this.revision,
  });

  final MealsApi api;
  final TodayMeals today;
  final ValueChanged<TodayMeal> onMealTap;

  /// Bir kayıt değişince artar; liste yeniden yüklenir.
  final int revision;

  @override
  State<DiaryScreen> createState() => _DiaryScreenState();
}

class _DiaryScreenState extends State<DiaryScreen> {
  List<MealLog>? _logs;
  Map<String, PlanMeal> _meals = const {};
  String? _error;

  @override
  void initState() {
    super.initState();
    _load();
  }

  @override
  void didUpdateWidget(DiaryScreen oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.revision != widget.revision) _load();
  }

  Future<void> _load() async {
    try {
      final plan = await widget.api.plan();
      final logs = await widget.api.logs();
      if (!mounted) return;
      setState(() {
        _error = null;
        _logs = logs;
        _meals = {for (final meal in plan?.meals ?? <PlanMeal>[]) meal.id: meal};
      });
    } on ApiException catch (error) {
      if (mounted) setState(() => _error = error.message);
    }
  }

  @override
  Widget build(BuildContext context) {
    final today = widget.today;
    final date = today.date;
    final done = today.meals.where((row) => row.log != null).length;
    final logged = _sum(
      today.meals.map(
        (row) => row.meal.totals.portion(_share(row.log?.status) * 100),
      ),
    );
    final history = [...?_logs]
      ..sort((a, b) => b.logDate.compareTo(a.logDate));
    final error = _error;
    return RefreshIndicator(
      onRefresh: _load,
      child: ListView(
        padding: const EdgeInsets.all(20),
        children: [
          ScreenHeading(
            '${dayLabel(date)} ${weekdays[date.weekday - 1]}'.toUpperCase(),
            'Öğün günlüğü',
            subtitle: 'Kayıtlarını incele ve gerekirse düzenle.',
          ),
          const SizedBox(height: 12),
          Panel(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text('Bugünkü kayıtlar', style: TextStyle(color: muted)),
                const SizedBox(height: 8),
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    Flexible(
                      child: Text(
                        '$done / ${today.meals.length} öğün',
                        style: Theme.of(context).textTheme.titleLarge,
                      ),
                    ),
                    const SizedBox(width: 8),
                    Text(
                      '${energyLabel(logged.kcal)} kcal',
                      style: const TextStyle(
                        color: teal,
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                  ],
                ),
              ],
            ),
          ),
          for (final row in today.meals) ...[
            const SizedBox(height: 10),
            Panel(
              padding: EdgeInsets.zero,
              child: ListTile(
                title: Text(
                  row.meal.name,
                  style: const TextStyle(fontWeight: FontWeight.bold),
                ),
                subtitle: Text(
                  '${mealTimeLabel(row.meal.time)} · ${energyLabel(row.meal.totals.kcal)} kcal'
                  '${row.log?.note?.isNotEmpty == true ? ' · ${row.log!.note}' : ''}',
                ),
                trailing: OutlinedButton(
                  onPressed: () => widget.onMealTap(row),
                  child: Text(row.log?.status.label ?? 'Kayıt ekle'),
                ),
                onTap: () => widget.onMealTap(row),
              ),
            ),
          ],
          const SizedBox(height: 16),
          Text('Son 7 gün', style: Theme.of(context).textTheme.titleMedium),
          const SizedBox(height: 8),
          if (error != null)
            Panel(
              child: Column(
                children: [
                  Text(error),
                  TextButton(onPressed: _load, child: const Text('Tekrar dene')),
                ],
              ),
            )
          else if (_logs == null)
            const Center(child: CircularProgressIndicator())
          else if (history.isEmpty)
            const Panel(child: Text('Henüz öğün kaydın yok.'))
          else
            Panel(
              child: Column(
                children: [
                  for (final log in history)
                    ListTile(
                      dense: true,
                      contentPadding: EdgeInsets.zero,
                      title: Text(_meals[log.mealId]?.name ?? 'Öğün'),
                      subtitle: Text(
                        [
                          '${dayLabel(log.logDate)} ${weekdays[log.logDate.weekday - 1]}',
                          if (log.note case final note? when note.isNotEmpty)
                            note,
                        ].join(' · '),
                      ),
                      trailing: Text(
                        log.hasPhoto
                            ? '${log.status.label} · Fotoğraf'
                            : log.status.label,
                        style: const TextStyle(color: teal),
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

/// "Diyet listem": yayımlanmış planın tamamı (GET /api/me/plan).
class PlanScreen extends StatefulWidget {
  const PlanScreen({super.key, required this.api});

  final MealsApi api;

  @override
  State<PlanScreen> createState() => _PlanScreenState();
}

class _PlanScreenState extends State<PlanScreen> {
  ClientPlan? _plan;
  bool _loading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final plan = await widget.api.plan();
      if (mounted) setState(() => _plan = plan);
    } on ApiException catch (error) {
      if (mounted) setState(() => _error = error.message);
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final plan = _plan;
    final error = _error;
    final heading = Theme.of(context).textTheme.titleMedium;
    return Scaffold(
      appBar: AppBar(title: const Text('Diyet listem')),
      body: SafeArea(
        child: ListView(
          padding: const EdgeInsets.all(20),
          children: [
            if (_loading)
              const Center(child: CircularProgressIndicator())
            else if (error != null)
              Panel(
                child: Column(
                  children: [
                    Text(error),
                    TextButton(
                      onPressed: _load,
                      child: const Text('Tekrar dene'),
                    ),
                  ],
                ),
              )
            else if (plan == null)
              const Panel(child: Text('Henüz yayımlanmış bir planın yok.'))
            else ...[
              ScreenHeading(
                plan.weekly ? 'HAFTALIK PLAN' : 'GÜNLÜK PLAN',
                plan.title,
                subtitle:
                    '${plan.meals.length} öğün · ${energyLabel(plan.totals.kcal)} kcal'
                    '${plan.weekly ? ' (haftalık toplam)' : ''}'
                    '${plan.targetKcal != null ? ' · Günlük hedef ${energyLabel(plan.targetKcal!)} kcal' : ''}',
              ),
              for (final meal in plan.meals) ...[
                const SizedBox(height: 12),
                Panel(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        mainAxisAlignment: MainAxisAlignment.spaceBetween,
                        children: [
                          Flexible(
                            child: Text(
                              plan.weekly
                                  ? '${weekdays[meal.dayNo - 1]} · ${meal.name}'
                                  : meal.name,
                              style: heading,
                            ),
                          ),
                          const SizedBox(width: 8),
                          Text(
                            mealTimeLabel(meal.time),
                            style: const TextStyle(
                              color: teal,
                              fontWeight: FontWeight.bold,
                            ),
                          ),
                        ],
                      ),
                      const SizedBox(height: 4),
                      Text(
                        '${energyLabel(meal.totals.kcal)} kcal · '
                        'K ${energyLabel(meal.totals.carbs)} g · '
                        'P ${energyLabel(meal.totals.protein)} g · '
                        'Y ${energyLabel(meal.totals.fat)} g',
                        style: const TextStyle(color: muted),
                      ),
                      const Divider(color: line),
                      _Portions(meal: meal),
                    ],
                  ),
                ),
              ],
              const SizedBox(height: 12),
              Text(
                plan.disclaimer,
                style: const TextStyle(color: muted, fontSize: 11),
              ),
            ],
          ],
        ),
      ),
    );
  }
}

/// Öğündeki besinler ve porsiyonlar; alternatifler "yerine" diye gösterilir.
class _Portions extends StatelessWidget {
  const _Portions({required this.meal});
  final PlanMeal meal;

  @override
  Widget build(BuildContext context) {
    String nameOf(String? id) => meal.items
        .where((item) => item.id == id)
        .map((item) => item.foodName)
        .firstOrNull ??
        '';
    return Column(
      children: [
        for (final item in meal.items)
          Padding(
            padding: const EdgeInsets.symmetric(vertical: 6),
            child: Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Flexible(
                  child: Text(
                    item.isAlternative
                        ? '${item.foodName} (${nameOf(item.alternativeToItemId)} yerine)'
                        : item.foodName,
                    style: item.isAlternative
                        ? const TextStyle(color: muted)
                        : null,
                  ),
                ),
                const SizedBox(width: 10),
                Flexible(
                  child: Text(
                    '${quantityLabel(item.portionCount)} × ${item.portionName} · ${quantityLabel(item.grams)} g',
                    textAlign: TextAlign.end,
                    style: const TextStyle(color: muted),
                  ),
                ),
              ],
            ),
          ),
      ],
    );
  }
}

/// Öğün kaydı + fotoğraf (UC-26, UC-27, sd-5).
class MealEditor extends StatefulWidget {
  const MealEditor({
    super.key,
    required this.api,
    required this.date,
    required this.row,
  });

  final MealsApi api;
  final DateTime date;
  final TodayMeal row;

  @override
  State<MealEditor> createState() => _MealEditorState();
}

class _MealEditorState extends State<MealEditor> {
  late final TextEditingController note;
  MealStatus? status;
  String? logId;
  bool hasPhoto = false;
  Uint8List? photo;
  String photoName = 'ogun.jpg';
  bool busy = false;
  String? error;
  String? photoError;

  @override
  void initState() {
    super.initState();
    final log = widget.row.log;
    note = TextEditingController(text: log?.note);
    status = log?.status;
    logId = log?.id;
    hasPhoto = log?.hasPhoto ?? false;
  }

  @override
  void dispose() {
    note.dispose();
    super.dispose();
  }

  Future<void> selectPhoto({bool fromCamera = false}) async {
    const maxBytes = 5 * 1024 * 1024;
    setState(() {
      busy = true;
      photoError = null;
    });
    try {
      final file = fromCamera
          ? await ImagePicker().pickImage(
              source: ImageSource.camera,
              maxWidth: 1600,
              maxHeight: 1600,
              imageQuality: 85,
            )
          : await openFile(
              acceptedTypeGroups: const [
                XTypeGroup(
                  label: 'JPEG ve PNG',
                  extensions: ['jpg', 'jpeg', 'png'],
                  mimeTypes: ['image/jpeg', 'image/png'],
                ),
              ],
            );
      if (file == null || !mounted) return;
      final length = await file.length();
      if (!mounted) return;
      if (length > maxBytes) {
        setState(() => photoError = 'Fotoğraf en fazla 5 MB olabilir.');
        return;
      }
      final bytes = await file.readAsBytes();
      if (!mounted) return;
      if (bytes.length > maxBytes) {
        setState(() => photoError = 'Fotoğraf en fazla 5 MB olabilir.');
        return;
      }
      // Sunucu da dosyanın içine bakar (BR-10); burada erkenden uyarıyoruz.
      const pngSignature = [137, 80, 78, 71, 13, 10, 26, 10];
      final isPng =
          bytes.length > 8 &&
          List.generate(
            8,
            (index) => bytes[index] == pngSignature[index],
          ).every((matches) => matches);
      final isJpeg =
          bytes.length > 3 &&
          bytes[0] == 255 &&
          bytes[1] == 216 &&
          bytes[2] == 255;
      if (!isPng && !isJpeg) {
        setState(
          () => photoError = 'Yalnızca JPEG ve PNG fotoğraf seçebilirsin.',
        );
        return;
      }
      final codec = await ui.instantiateImageCodec(
        bytes,
        targetWidth: 560,
        allowUpscaling: false,
      );
      try {
        final frame = await codec.getNextFrame();
        frame.image.dispose();
      } finally {
        codec.dispose();
      }
      if (mounted) {
        setState(() {
          photo = bytes;
          photoName = isPng ? 'ogun.png' : 'ogun.jpg';
        });
      }
    } on Exception {
      if (mounted) {
        setState(
          () => photoError = 'Fotoğraf açılamadı. Başka bir JPEG veya PNG seç.',
        );
      }
    } finally {
      if (mounted) setState(() => busy = false);
    }
  }

  /// Önce kayıt (yoksa oluşturulur, varsa güncellenir), sonra seçilen fotoğraf
  /// yüklenir. Fotoğraf yüklenemezse kayıt yine durur; tekrar denenebilir.
  Future<void> save() async {
    final selected = status;
    if (selected == null || busy) return;
    setState(() {
      busy = true;
      error = null;
    });
    try {
      final text = note.text.trim().isEmpty ? null : note.text.trim();
      var id = logId;
      if (id == null) {
        id = await widget.api.log(
          mealId: widget.row.meal.id,
          date: widget.date,
          status: selected,
          note: text,
        );
        logId = id;
      } else {
        await widget.api.update(id, status: selected, note: text);
      }
      final bytes = photo;
      if (bytes != null && !hasPhoto) {
        await widget.api.uploadPhoto(id, bytes, photoName);
        hasPhoto = true;
      }
      if (mounted) Navigator.pop(context, true);
    } on ApiException catch (failure) {
      if (mounted) setState(() => error = failure.message);
    } finally {
      if (mounted) setState(() => busy = false);
    }
  }

  Future<void> showPhoto() async {
    final id = logId;
    if (id == null) return;
    setState(() {
      busy = true;
      photoError = null;
    });
    try {
      final url = await widget.api.photoUrl(id);
      if (!mounted) return;
      await showDialog<void>(
        context: context,
        builder: (context) => AlertDialog(
          title: const Text('Öğün fotoğrafı'),
          content: Image.network(
            url,
            fit: BoxFit.contain,
            errorBuilder: (context, error, stackTrace) => const Text(
              'Fotoğraf açılamadı. Bağlantı 10 dakika geçerlidir; '
              'telefonun fotoğraf deposuna ulaşabildiğini kontrol edin.',
            ),
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(context),
              child: const Text('Kapat'),
            ),
          ],
        ),
      );
    } on ApiException catch (failure) {
      if (mounted) setState(() => photoError = failure.message);
    } finally {
      if (mounted) setState(() => busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final meal = widget.row.meal;
    final heading = Theme.of(context).textTheme.titleMedium;
    final selected = status;
    final failure = error;
    return Scaffold(
      appBar: AppBar(title: Text(meal.name)),
      body: SafeArea(
        child: ListView(
          padding: const EdgeInsets.all(20),
          children: [
            ScreenHeading(
              '${mealTimeLabel(meal.time)} · ${meal.name}',
              'Öğün kaydı',
              subtitle: 'Porsiyonları kontrol et ve öğününü işaretle.',
            ),
            const SizedBox(height: 12),
            Panel(
              child: Column(
                children: [
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      Flexible(
                        child: Text('Planlanan porsiyonlar', style: heading),
                      ),
                      Text(
                        '${energyLabel(meal.totals.kcal)} kcal',
                        style: const TextStyle(
                          color: teal,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 8),
                  _Portions(meal: meal),
                ],
              ),
            ),
            const SizedBox(height: 12),
            Panel(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text('Öğünü nasıl tamamladın?', style: heading),
                  const SizedBox(height: 8),
                  Wrap(
                    spacing: 6,
                    children: [
                      for (final value in MealStatus.values)
                        ChoiceChip(
                          label: Text(value.label),
                          selected: selected == value,
                          onSelected: busy
                              ? null
                              : (_) => setState(() => status = value),
                        ),
                    ],
                  ),
                  if (selected != null)
                    Text(
                      'Kayda eklenecek tahmini enerji ${energyLabel(meal.totals.kcal * _share(selected))} kcal',
                      style: const TextStyle(color: teal, fontSize: 12),
                    ),
                ],
              ),
            ),
            const SizedBox(height: 12),
            Panel(
              child: TextField(
                controller: note,
                maxLines: 2,
                maxLength: 500,
                enabled: !busy,
                decoration: const InputDecoration(
                  labelText: 'Not ekle (isteğe bağlı)',
                  border: InputBorder.none,
                ),
              ),
            ),
            const SizedBox(height: 12),
            Panel(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Text('Öğün fotoğrafı', style: heading),
                  const SizedBox(height: 4),
                  const Text(
                    'İsteğe bağlı · Bir JPEG veya PNG · En fazla 5 MB',
                    style: TextStyle(color: muted, fontSize: 12),
                  ),
                  if (hasPhoto)
                    Align(
                      alignment: Alignment.centerLeft,
                      child: TextButton.icon(
                        onPressed: busy ? null : showPhoto,
                        icon: const Icon(Icons.photo_outlined),
                        label: const Text('Fotoğraf eklendi · Görüntüle'),
                      ),
                    )
                  else ...[
                    if (photo case final bytes?) ...[
                      const SizedBox(height: 10),
                      ClipRRect(
                        borderRadius: BorderRadius.circular(8),
                        child: Image.memory(
                          bytes,
                          height: 150,
                          cacheWidth: 560,
                          fit: BoxFit.contain,
                          semanticLabel: 'Öğün fotoğrafı',
                        ),
                      ),
                    ],
                    Wrap(
                      spacing: 6,
                      children: [
                        TextButton.icon(
                          onPressed: busy
                              ? null
                              : () => selectPhoto(fromCamera: true),
                          icon: const Icon(Icons.photo_camera_outlined),
                          label: const Text('Fotoğraf çek'),
                        ),
                        TextButton.icon(
                          onPressed: busy ? null : selectPhoto,
                          icon: const Icon(Icons.add_photo_alternate_outlined),
                          label: Text(
                            photo == null
                                ? 'Fotoğraf seç'
                                : 'Fotoğrafı değiştir',
                          ),
                        ),
                        if (photo != null)
                          TextButton(
                            onPressed: busy
                                ? null
                                : () => setState(() {
                                    photo = null;
                                    photoError = null;
                                  }),
                            child: const Text('Fotoğrafı kaldır'),
                          ),
                      ],
                    ),
                  ],
                  if (photoError case final message?)
                    Text(
                      message,
                      style: TextStyle(
                        color: Theme.of(context).colorScheme.error,
                      ),
                    ),
                ],
              ),
            ),
            if (failure != null) ...[
              const SizedBox(height: 12),
              Text(
                failure,
                style: TextStyle(color: Theme.of(context).colorScheme.error),
              ),
            ],
            const SizedBox(height: 12),
            Align(
              child: FilledButton(
                onPressed: selected == null || busy ? null : save,
                child: busy
                    ? const SizedBox.square(
                        dimension: 20,
                        child: CircularProgressIndicator(strokeWidth: 2),
                      )
                    : const Text('Öğünü kaydet'),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
