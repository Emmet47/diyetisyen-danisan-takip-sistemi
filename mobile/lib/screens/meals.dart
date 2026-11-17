import 'dart:math' as math;
import 'dart:typed_data';
import 'dart:ui' as ui;

import 'package:file_selector/file_selector.dart';

import 'package:flutter/material.dart';
import 'package:image_picker/image_picker.dart';

import '../demo_data.dart';
import '../features/foods/food_catalog.dart';
import '../features/foods/food_picker.dart';
import '../ui.dart';

class TodayScreen extends StatelessWidget {
  const TodayScreen({
    super.key,
    required this.entries,
    required this.onMealTap,
  });

  final Map<Meal, MealEntry> entries;
  final ValueChanged<Meal> onMealTap;

  @override
  Widget build(BuildContext context) {
    final today = DateTime.now();
    final total = loggedNutrition(entries);
    final remaining = dailyTarget.kcal - total.kcal;
    final ratio = total.kcal / dailyTarget.kcal;
    final heading = Theme.of(context).textTheme.titleMedium;
    return ListView(
      padding: const EdgeInsets.all(20),
      children: [
        ScreenHeading(
          '${dayLabel(today)} ${weekdays[today.weekday - 1]}',
          'Beslenme günlüğü',
        ),
        const SizedBox(height: 8),
        Align(
          alignment: Alignment.centerLeft,
          child: OutlinedButton.icon(
            onPressed: () => Navigator.push<void>(
              context,
              MaterialPageRoute(builder: (_) => _DietPlanScreen(day: today)),
            ),
            icon: const Icon(Icons.list_alt_outlined),
            label: const Text('Diyet listem'),
          ),
        ),
        const SizedBox(height: 12),
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
                              remaining < 0 ? 'kcal hedef üstü' : 'kcal kaldı',
                              large: true,
                            ),
                          ),
                        ),
                      ),
                    ),
                  ),
                  Expanded(
                    child: _EnergyValue(energyLabel(dailyTarget.kcal), 'Hedef'),
                  ),
                ],
              ),
              const SizedBox(height: 6),
              Text(
                '${energyLabel(total.kcal)} kcal alındı · Hedefin %${(ratio * 100).round()}’si',
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
              '${entries.length} / ${meals.length} kaydedildi',
              style: const TextStyle(color: teal, fontSize: 12),
            ),
          ],
        ),
        const SizedBox(height: 8),
        for (final (index, meal) in meals.indexed) ...[
          if (index > 0) const SizedBox(height: 8),
          _MealTile(
            meal: meal,
            index: index,
            entry: entries[meal],
            onTap: () => onMealTap(meal),
          ),
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
                    'Alınan / hedef',
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
                      dailyTarget.carbs,
                      const Color(0xff1eaeb1),
                    ),
                  ),
                  const SizedBox(width: 10),
                  Expanded(
                    child: _Macro(
                      'Protein',
                      total.protein,
                      dailyTarget.protein,
                      const Color(0xffad7123),
                    ),
                  ),
                  const SizedBox(width: 10),
                  Expanded(
                    child: _Macro(
                      'Yağ',
                      total.fat,
                      dailyTarget.fat,
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
          'Değerler örnek gün kaydına aittir.',
          style: TextStyle(color: muted, fontSize: 11),
        ),
      ],
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
  const _MealTile({
    required this.meal,
    required this.index,
    required this.entry,
    required this.onTap,
  });
  final Meal meal;
  final int index;
  final MealEntry? entry;
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
    final calories = mealNutrition(
      meal,
      entry?.portionPercent ?? 100,
      entry?.foods ?? const [],
    ).kcal;
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
            color: backgrounds[index],
            borderRadius: BorderRadius.circular(12),
          ),
          child: Icon(icons[index], size: 22, color: teal),
        ),
        title: Text(
          meal.title,
          style: const TextStyle(fontWeight: FontWeight.w700),
        ),
        subtitle: Text(
          '${meal.time} · ${entry?.status.label ?? 'Planlı'}',
          style: const TextStyle(fontSize: 12, color: muted),
        ),
        trailing: Row(
          mainAxisSize: MainAxisSize.min,
          children: [
            Text(
              '${energyLabel(calories)} kcal',
              style: const TextStyle(
                fontSize: 12,
                fontWeight: FontWeight.w700,
                color: teal,
              ),
            ),
            const SizedBox(width: 6),
            Icon(
              entry == null ? Icons.add_circle_outline : Icons.chevron_right,
              color: teal,
              size: 20,
              semanticLabel: entry == null ? 'Kayıt ekle' : 'Kaydı düzenle',
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
        value: (value / target).clamp(0.0, 1.0),
        color: color,
        backgroundColor: line,
        minHeight: 5,
        borderRadius: BorderRadius.circular(3),
        semanticsLabel: label,
      ),
    ],
  );
}

class DiaryScreen extends StatelessWidget {
  const DiaryScreen({
    super.key,
    required this.entries,
    required this.onMealTap,
  });

  final Map<Meal, MealEntry> entries;
  final ValueChanged<Meal> onMealTap;

  @override
  Widget build(BuildContext context) {
    final today = DateTime.now();
    return ListView(
      padding: const EdgeInsets.all(20),
      children: [
        ScreenHeading(
          '${dayLabel(today)} ${weekdays[today.weekday - 1]}'.toUpperCase(),
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
                      '${entries.length} / ${meals.length} öğün',
                      style: Theme.of(context).textTheme.titleLarge,
                    ),
                  ),
                  const SizedBox(width: 8),
                  Text(
                    '${energyLabel(loggedNutrition(entries).kcal)} kcal',
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
        for (final meal in meals) ...[
          const SizedBox(height: 10),
          Panel(
            padding: EdgeInsets.zero,
            child: ListTile(
              title: Text(
                meal.title,
                style: const TextStyle(fontWeight: FontWeight.bold),
              ),
              subtitle: Text(
                '${meal.time} · ${_mealEnergyLabel(meal, entries[meal])}',
              ),
              trailing: OutlinedButton(
                onPressed: () => onMealTap(meal),
                child: Text(entries[meal]?.status.label ?? 'Kayıt ekle'),
              ),
              onTap: () => onMealTap(meal),
            ),
          ),
        ],
      ],
    );
  }
}

String _mealEnergyLabel(Meal meal, MealEntry? entry) {
  if (entry == null) return 'Planlı';
  final nutrition = mealNutrition(meal, entry.portionPercent, entry.foods);
  return '${energyLabel(nutrition.kcal)} kcal';
}

class _DietPlanScreen extends StatelessWidget {
  const _DietPlanScreen({required this.day});
  final DateTime day;

  @override
  Widget build(BuildContext context) {
    final planEnergy = meals.fold<double>(
      0,
      (total, meal) => total + meal.nutrition.kcal,
    );
    final heading = Theme.of(context).textTheme.titleMedium;
    return Scaffold(
      appBar: AppBar(title: const Text('Diyet listem')),
      body: SafeArea(
        child: ListView(
          padding: const EdgeInsets.all(20),
          children: [
            ScreenHeading(
              '${dayLabel(day)} ${weekdays[day.weekday - 1]}',
              'Günlük beslenme planı',
              subtitle:
                  'Örnek plan · ${meals.length} öğün · ${energyLabel(planEnergy)} kcal',
            ),
            for (final meal in meals) ...[
              const SizedBox(height: 12),
              Panel(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        Flexible(child: Text(meal.title, style: heading)),
                        const SizedBox(width: 8),
                        Text(
                          meal.time,
                          style: const TextStyle(
                            color: teal,
                            fontWeight: FontWeight.bold,
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: 4),
                    Text(
                      '${energyLabel(meal.nutrition.kcal)} kcal',
                      style: const TextStyle(color: muted),
                    ),
                    const Divider(color: line),
                    _Portions(meal: meal),
                  ],
                ),
              ),
            ],
          ],
        ),
      ),
    );
  }
}

class _Portions extends StatelessWidget {
  const _Portions({required this.meal});
  final Meal meal;

  @override
  Widget build(BuildContext context) => Column(
    children: [
      for (final food in meal.portions.split('\n'))
        Padding(
          padding: const EdgeInsets.symmetric(vertical: 6),
          child: Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              Flexible(child: Text(food.split(' · ').first)),
              const SizedBox(width: 10),
              Flexible(
                child: Text(
                  food.split(' · ').last,
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

class MealEditor extends StatefulWidget {
  const MealEditor({super.key, required this.meal, this.entry});

  final Meal meal;
  final MealEntry? entry;

  @override
  State<MealEditor> createState() => _MealEditorState();
}

class _MealEditorState extends State<MealEditor> {
  late final TextEditingController note;
  late final _foods = [...?widget.entry?.foods];
  MealStatus? status;
  int partialPercent = 50;
  Uint8List? photo;
  bool pickingPhoto = false;
  String? photoError;

  Future<void> _openFoodPicker() async {
    final item = await Navigator.push<LoggedFood>(
      context,
      MaterialPageRoute(
        builder: (_) =>
            FoodPicker(mealName: widget.meal.title, selected: _foods),
      ),
    );
    if (!mounted || item == null) return;
    setState(() {
      final index = _foods.indexWhere((entry) => entry.food.id == item.food.id);
      if (index < 0) {
        _foods.add(item);
      } else {
        _foods[index] = item;
      }
    });
  }

  Future<void> selectPhoto({bool fromCamera = false}) async {
    const maxBytes = 5 * 1024 * 1024;
    setState(() {
      pickingPhoto = true;
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
      if (mounted) setState(() => photo = bytes);
    } on Exception {
      if (mounted) {
        setState(
          () => photoError = 'Fotoğraf açılamadı. Başka bir JPEG veya PNG seç.',
        );
      }
    } finally {
      if (mounted) setState(() => pickingPhoto = false);
    }
  }

  @override
  void initState() {
    super.initState();
    note = TextEditingController(text: widget.entry?.note);
    status = widget.entry?.status;
    photo = widget.entry?.photo;
    if (status == MealStatus.partial) {
      partialPercent = widget.entry!.portionPercent;
    }
  }

  @override
  void dispose() {
    note.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final meal = widget.meal;
    final heading = Theme.of(context).textTheme.titleMedium;
    final portion = switch (status) {
      MealStatus.eaten => 100,
      MealStatus.partial => partialPercent,
      MealStatus.skipped => 0,
      null => null,
    };
    return Scaffold(
      appBar: AppBar(title: Text(meal.title)),
      body: SafeArea(
        child: ListView(
          padding: const EdgeInsets.all(20),
          children: [
            ScreenHeading(
              '${meal.time} · ${meal.title}',
              'Öğün kaydı',
              subtitle: 'Porsiyonları kontrol et ve öğününü işaretle.',
            ),
            const SizedBox(height: 12),
            Panel(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Row(
                    children: [
                      Expanded(child: Text('Eklenen besinler', style: heading)),
                      OutlinedButton.icon(
                        onPressed: _openFoodPicker,
                        icon: const Icon(Icons.add, size: 18),
                        label: const Text('Besin ekle'),
                      ),
                    ],
                  ),
                  for (final item in _foods)
                    ListTile(
                      contentPadding: EdgeInsets.zero,
                      title: Text(item.food.name),
                      subtitle: Text(
                        '${quantityLabel(item.portions)} porsiyon · '
                        '${quantityLabel(item.food.portionGrams * item.portions)} g · '
                        '${energyLabel(item.food.forPortions(item.portions).kcal)} kcal',
                      ),
                      trailing: IconButton(
                        tooltip: '${item.food.name} besinini kaldır',
                        icon: const Icon(Icons.remove_circle_outline),
                        onPressed: () => setState(() => _foods.remove(item)),
                      ),
                    ),
                ],
              ),
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
                        '${energyLabel(meal.nutrition.kcal)} kcal',
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
                          selected: status == value,
                          onSelected: (_) => setState(() => status = value),
                        ),
                    ],
                  ),
                  if (status == MealStatus.partial) ...[
                    Text('Tüketilen planlı porsiyon %$partialPercent'),
                    Slider(
                      min: 5,
                      max: 95,
                      divisions: 18,
                      value: partialPercent.toDouble(),
                      label: '%$partialPercent',
                      onChanged: (value) =>
                          setState(() => partialPercent = value.round()),
                    ),
                  ],
                  if (portion != null)
                    Text(
                      'Kayda eklenecek enerji ${energyLabel(mealNutrition(meal, portion, _foods).kcal)} kcal',
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
                maxLength: 300,
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
                        onPressed: pickingPhoto
                            ? null
                            : () => selectPhoto(fromCamera: true),
                        icon: const Icon(Icons.photo_camera_outlined),
                        label: const Text('Fotoğraf çek'),
                      ),
                      TextButton.icon(
                        onPressed: pickingPhoto ? null : selectPhoto,
                        icon: const Icon(Icons.add_photo_alternate_outlined),
                        label: Text(
                          photo == null ? 'Fotoğraf seç' : 'Fotoğrafı değiştir',
                        ),
                      ),
                      if (photo != null)
                        TextButton(
                          onPressed: pickingPhoto
                              ? null
                              : () => setState(() {
                                  photo = null;
                                  photoError = null;
                                }),
                          child: const Text('Fotoğrafı kaldır'),
                        ),
                    ],
                  ),
                  if (photoError case final error?)
                    Text(
                      error,
                      style: TextStyle(
                        color: Theme.of(context).colorScheme.error,
                      ),
                    ),
                ],
              ),
            ),
            const SizedBox(height: 12),
            Align(
              child: FilledButton(
                onPressed: portion == null || pickingPhoto
                    ? null
                    : () => Navigator.pop(context, (
                        status: status!,
                        note: note.text.trim(),
                        portionPercent: portion,
                        photo: photo,
                        foods: List<LoggedFood>.unmodifiable(_foods),
                      )),
                child: const Text('Öğünü kaydet'),
              ),
            ),
          ],
        ),
      ),
    );
  }
}
