class Nutrition {
  const Nutrition(this.carbs, this.protein, this.fat, {this.energyKcal});

  final double carbs;
  final double protein;
  final double fat;
  final double? energyKcal;
  double get kcal => energyKcal ?? 4 * (carbs + protein) + 9 * fat;

  Nutrition operator +(Nutrition other) => Nutrition(
    carbs + other.carbs,
    protein + other.protein,
    fat + other.fat,
    energyKcal: kcal + other.kcal,
  );

  Nutrition portion(num percent) {
    final factor = percent / 100;
    return Nutrition(
      carbs * factor,
      protein * factor,
      fat * factor,
      energyKcal: kcal * factor,
    );
  }
}
