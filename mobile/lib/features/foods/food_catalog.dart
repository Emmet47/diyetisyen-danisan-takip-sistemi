import '../../core/nutrition.dart';

class FoodItem {
  const FoodItem(
    this.id,
    this.name,
    this.portionName,
    this.portionGrams,
    this.per100Grams,
  );

  final String id;
  final String name;
  final String portionName;
  final int portionGrams;
  final Nutrition per100Grams;

  Nutrition forPortions(double count) =>
      per100Grams.portion(portionGrams * count);
}

typedef LoggedFood = ({FoodItem food, double portions});

String quantityLabel(num value) => value
    .toStringAsFixed(2)
    .replaceFirst(RegExp(r'\.?0+$'), '')
    .replaceAll('.', ',');

// Demonstration catalog. Values are sample data per 100 g.
const foodCatalog = [
  FoodItem(
    'almond',
    'Çiğ badem',
    '1 avuç',
    25,
    Nutrition(22, 21, 50, energyKcal: 579),
  ),
  FoodItem(
    'apple',
    'Elma',
    '1 adet',
    150,
    Nutrition(14, 0.3, 0.2, energyKcal: 52),
  ),
  FoodItem(
    'banana',
    'Muz',
    '1 adet',
    120,
    Nutrition(23, 1.1, 0.3, energyKcal: 89),
  ),
  FoodItem(
    'bulgur',
    'Bulgur pilavı',
    '1 yemek kaşığı',
    25,
    Nutrition(19, 3, 3, energyKcal: 115),
  ),
  FoodItem(
    'chicken',
    'Izgara tavuk göğsü',
    '1 porsiyon',
    120,
    Nutrition(0, 31, 3.6, energyKcal: 165),
  ),
  FoodItem(
    'egg',
    'Haşlanmış yumurta',
    '1 adet',
    50,
    Nutrition(1.1, 13, 11, energyKcal: 155),
  ),
  FoodItem(
    'lentil-soup',
    'Mercimek çorbası',
    '1 kase',
    200,
    Nutrition(10, 4, 2, energyKcal: 74),
  ),
  FoodItem(
    'oats',
    'Yulaf ezmesi',
    '1 yemek kaşığı',
    10,
    Nutrition(66, 17, 7, energyKcal: 389),
  ),
  FoodItem(
    'salad',
    'Mevsim salatası',
    '1 kase',
    150,
    Nutrition(5, 1.3, 0.3, energyKcal: 28),
  ),
  FoodItem(
    'tomato',
    'Domates',
    '1 adet',
    120,
    Nutrition(3.9, 0.9, 0.2, energyKcal: 18),
  ),
  FoodItem(
    'wheat-bread',
    'Tam buğday ekmeği',
    '1 dilim',
    30,
    Nutrition(43, 13, 4, energyKcal: 247),
  ),
  FoodItem(
    'yogurt',
    'Yoğurt',
    '1 kase',
    150,
    Nutrition(4.7, 3.5, 3.3, energyKcal: 61),
  ),
];

String _normalize(String value) => value
    .trim()
    .replaceAll(RegExp('[ıİ]'), 'i')
    .toLowerCase()
    .replaceAll('ç', 'c')
    .replaceAll('ğ', 'g')
    .replaceAll('ö', 'o')
    .replaceAll('ş', 's')
    .replaceAll('ü', 'u');

List<FoodItem> searchFoods(String query) {
  final terms = _normalize(query).split(RegExp(r'\s+'));
  return [
    for (final food in foodCatalog)
      if (terms.every(_normalize(food.name).contains)) food,
  ];
}
