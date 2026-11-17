import 'dart:typed_data';

import 'core/nutrition.dart';
import 'features/foods/food_catalog.dart';

export 'core/nutrition.dart';

enum MealStatus {
  eaten('Yedim'),
  partial('Kısmen yedim'),
  skipped('Yemedim');

  const MealStatus(this.label);
  final String label;
}

typedef Meal = ({
  String title,
  String time,
  String portions,
  Nutrition nutrition,
});
typedef MealEntry = ({
  MealStatus status,
  String note,
  int portionPercent,
  Uint8List? photo,
  List<LoggedFood> foods,
});
typedef Appointment = ({DateTime time, String status});

const dailyTarget = Nutrition(200, 115, 60);

Nutrition mealNutrition(Meal meal, int percent, List<LoggedFood> foods) {
  return foods.fold(
    meal.nutrition.portion(percent),
    (total, entry) => total + entry.food.forPortions(entry.portions),
  );
}

Nutrition loggedNutrition(Map<Meal, MealEntry> entries) {
  return entries.entries.fold(
    const Nutrition(0, 0, 0),
    (total, item) =>
        total +
        mealNutrition(item.key, item.value.portionPercent, item.value.foods),
  );
}

String energyLabel(num value) {
  final digits = value.round().abs().toString();
  final grouped = digits.replaceAllMapped(
    RegExp(r'\B(?=(\d{3})+(?!\d))'),
    (_) => '.',
  );
  return value.round() < 0 ? '−$grouped' : grouped;
}

const meals = <Meal>[
  (
    title: 'Kahvaltı',
    time: '08.30',
    nutrition: Nutrition(50, 31, 14),
    portions: 'Yulaf · 4 yemek kaşığı\nYoğurt · 1 kase\nMuz · ½ adet',
  ),
  (
    title: 'Öğle yemeği',
    time: '12.30',
    nutrition: Nutrition(60, 37, 18),
    portions: 'Mercimek çorbası · 1 kase\nBulgur pilavı · 4 yemek kaşığı\nMevsim salatası · 1 porsiyon',
  ),
  (
    title: 'Ara öğün',
    time: '15.30',
    nutrition: Nutrition(20, 12, 8),
    portions: 'Elma · 1 adet\nÇiğ badem · 8 adet',
  ),
  (
    title: 'Akşam yemeği',
    time: '19.00',
    nutrition: Nutrition(70, 35, 20),
    portions: 'Sebze yemeği · 1 porsiyon\nYoğurt · 1 kase',
  ),
];

const weekdays = [
  'Pazartesi',
  'Salı',
  'Çarşamba',
  'Perşembe',
  'Cuma',
  'Cumartesi',
  'Pazar',
];
const months = [
  'Ocak',
  'Şubat',
  'Mart',
  'Nisan',
  'Mayıs',
  'Haziran',
  'Temmuz',
  'Ağustos',
  'Eylül',
  'Ekim',
  'Kasım',
  'Aralık',
];

String dayLabel(DateTime date) => '${date.day} ${months[date.month - 1]}';
String timeLabel(DateTime date) =>
    '${date.hour.toString().padLeft(2, '0')}:${date.minute.toString().padLeft(2, '0')}';
