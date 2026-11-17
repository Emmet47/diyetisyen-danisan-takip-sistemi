import 'dart:typed_data';

import '../../core/api_client.dart';
import '../../core/format.dart';
import '../../core/json.dart';
import '../../core/nutrition.dart';

/// Öğün kaydı durumu (FR-G1). [apiValue] API'deki enum adıdır.
enum MealStatus {
  eaten('Yedim', 'Eaten'),
  partial('Kısmen yedim', 'Partial'),
  skipped('Yemedim', 'Skipped');

  const MealStatus(this.label, this.apiValue);
  final String label;
  final String apiValue;

  static MealStatus parse(String value) => MealStatus.values.firstWhere(
    (status) => status.apiValue == value,
    orElse: () => throw const FormatException(),
  );
}

/// API'deki NutrientTotals → enerji ve makrolar.
Nutrition readTotals(Object? json) {
  final map = asMap(json);
  return Nutrition(
    readNumber(map, 'carbsG'),
    readNumber(map, 'proteinG'),
    readNumber(map, 'fatG'),
    energyKcal: readNumber(map, 'energyKcal'),
  );
}

class PlanItem {
  const PlanItem({
    required this.id,
    required this.foodName,
    required this.portionName,
    required this.portionCount,
    required this.grams,
    required this.isAlternative,
    required this.alternativeToItemId,
  });

  final String id;
  final String foodName;
  final String portionName;
  final double portionCount;
  final double grams;
  final bool isAlternative;
  final String? alternativeToItemId;

  factory PlanItem.fromJson(Object? json) {
    final map = asMap(json);
    return PlanItem(
      id: readString(map, 'id'),
      foodName: readString(map, 'foodName'),
      portionName: readString(map, 'portionName'),
      portionCount: readNumber(map, 'portionCount'),
      grams: readNumber(map, 'grams'),
      isAlternative: readBool(map, 'isAlternative'),
      alternativeToItemId: readOptionalString(map, 'alternativeToItemId'),
    );
  }
}

/// Plandaki bir öğün: plan → öğün → besin satırı (Composite).
class PlanMeal {
  const PlanMeal({
    required this.id,
    required this.dayNo,
    required this.name,
    required this.time,
    required this.items,
    required this.totals,
  });

  final String id;
  final int dayNo;
  final String name;

  /// Türkiye saati, "08:00:00".
  final String time;
  final List<PlanItem> items;
  final Nutrition totals;

  factory PlanMeal.fromJson(Object? json) {
    final map = asMap(json);
    return PlanMeal(
      id: readString(map, 'id'),
      dayNo: readNumber(map, 'dayNo').round(),
      name: readString(map, 'name'),
      time: readString(map, 'time'),
      items: [for (final item in asList(map['items'])) PlanItem.fromJson(item)],
      totals: readTotals(map['totals']),
    );
  }
}

class MealLog {
  const MealLog({
    required this.id,
    required this.mealId,
    required this.logDate,
    required this.status,
    required this.note,
    required this.hasPhoto,
  });

  final String id;
  final String mealId;
  final DateTime logDate;
  final MealStatus status;
  final String? note;
  final bool hasPhoto;

  factory MealLog.fromJson(Object? json) {
    final map = asMap(json);
    return MealLog(
      id: readString(map, 'id'),
      mealId: readString(map, 'mealId'),
      logDate: parseDate(readString(map, 'logDate')),
      status: MealStatus.parse(readString(map, 'status')),
      note: readOptionalString(map, 'note'),
      hasPhoto: readBool(map, 'hasPhoto'),
    );
  }
}

typedef TodayMeal = ({PlanMeal meal, MealLog? log});

/// GET /api/me/meals/today
class TodayMeals {
  const TodayMeals({
    required this.date,
    required this.planTitle,
    required this.meals,
  });

  final DateTime date;
  final String? planTitle;
  final List<TodayMeal> meals;

  bool get hasPlan => planTitle != null;

  factory TodayMeals.fromJson(Object? json) {
    final map = asMap(json);
    return TodayMeals(
      date: parseDate(readString(map, 'date')),
      planTitle: readOptionalString(map, 'planTitle'),
      meals: [
        for (final row in asList(map['meals']))
          (
            meal: PlanMeal.fromJson(asMap(row)['meal']),
            log: asMap(row)['log'] == null
                ? null
                : MealLog.fromJson(asMap(row)['log']),
          ),
      ],
    );
  }
}

/// GET /api/me/plan: danışanın yayımlanmış planı (FR-E5).
class ClientPlan {
  const ClientPlan({
    required this.title,
    required this.weekly,
    required this.targetKcal,
    required this.meals,
    required this.totals,
    required this.disclaimer,
  });

  final String title;
  final bool weekly;
  final double? targetKcal;
  final List<PlanMeal> meals;
  final Nutrition totals;
  final String disclaimer;

  factory ClientPlan.fromJson(Object? json) {
    final map = asMap(json);
    return ClientPlan(
      title: readString(map, 'title'),
      weekly: readString(map, 'planType') == 'Weekly',
      targetKcal: readOptionalNumber(map, 'targetKcal'),
      meals: [for (final meal in asList(map['meals'])) PlanMeal.fromJson(meal)],
      totals: readTotals(map['totals']),
      disclaimer: readString(map, 'disclaimer'),
    );
  }
}

/// Danışanın öğün uçları: /api/me/... (sd-5).
class MealsApi {
  const MealsApi(this.session);

  final SessionApi session;

  Future<TodayMeals> today() async =>
      readResponse(await session.get('/api/me/meals/today'), TodayMeals.fromJson);

  /// Yayımlanmış plan yoksa null.
  Future<ClientPlan?> plan() async {
    try {
      return readResponse(await session.get('/api/me/plan'), ClientPlan.fromJson);
    } on ApiException catch (error) {
      if (error.statusCode == 404) return null;
      rethrow;
    }
  }

  /// Son kayıtlar (varsayılan son 7 gün).
  Future<List<MealLog>> logs() async => readResponse(
    await session.get('/api/me/meal-logs'),
    (json) => [for (final row in asList(json)) MealLog.fromJson(row)],
  );

  /// Yeni kayıt; aynı öğün aynı gün ikinci kez kaydedilemez (409).
  Future<String> log({
    required String mealId,
    required DateTime date,
    required MealStatus status,
    required String? note,
  }) async => readResponse(
    await session.post('/api/me/meal-logs', {
      'mealId': mealId,
      'logDate': dateKey(date),
      'status': status.apiValue,
      'note': note,
    }),
    (json) => readString(asMap(json), 'id'),
  );

  Future<void> update(
    String id, {
    required MealStatus status,
    required String? note,
  }) => session.put('/api/me/meal-logs/$id', {
    'status': status.apiValue,
    'note': note,
  });

  /// JPEG/PNG, en fazla 5 MB; kayıt başına bir fotoğraf (FR-G2).
  Future<void> uploadPhoto(String id, Uint8List bytes, String filename) =>
      session.upload('/api/me/meal-logs/$id/photo', bytes, filename);

  /// 10 dakika geçerli imzalı bağlantı.
  Future<String> photoUrl(String id) async => readResponse(
    await session.get('/api/meal-logs/$id/photo-url'),
    (json) => readString(asMap(json), 'url'),
  );
}
