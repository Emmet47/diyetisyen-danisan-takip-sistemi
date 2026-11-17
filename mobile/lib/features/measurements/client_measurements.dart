import '../../core/api_client.dart';

class MeasurementHistory {
  const MeasurementHistory({
    required this.items,
    this.targetWeightKg,
    this.remainingKg,
  });

  final List<Measurement> items;
  final double? targetWeightKg;
  final double? remainingKg;

  factory MeasurementHistory.fromJson(Map<String, dynamic> json) {
    final items = json['items'];
    if (items is! List) throw const FormatException();

    return MeasurementHistory(
      items: [
        for (final item in items)
          if (item is Map<String, dynamic>)
            Measurement.fromJson(item)
          else
            throw const FormatException(),
      ],
      targetWeightKg: _number(json, 'targetWeightKg'),
      remainingKg: _number(json, 'remainingKg'),
    );
  }
}

class Measurement {
  const Measurement({
    required this.measuredOn,
    required this.weightKg,
    required this.heightCm,
    required this.waistCm,
    this.hipCm,
    this.bodyFatPercent,
    this.note,
    this.weightChangeKg,
    this.waistChangeCm,
  });

  final DateTime measuredOn;
  final double weightKg;
  final double heightCm;
  final double waistCm;
  final double? hipCm;
  final double? bodyFatPercent;
  final String? note;
  final double? weightChangeKg;
  final double? waistChangeCm;

  factory Measurement.fromJson(Map<String, dynamic> json) {
    final date = json['measuredOn'];
    final measuredOn = date is String ? DateTime.tryParse(date) : null;
    final note = json['note'];
    if (measuredOn == null || note != null && note is! String) {
      throw const FormatException();
    }

    return Measurement(
      measuredOn: measuredOn,
      weightKg: _number(json, 'weightKg') ?? (throw const FormatException()),
      heightCm: _number(json, 'heightCm') ?? (throw const FormatException()),
      waistCm: _number(json, 'waistCm') ?? (throw const FormatException()),
      hipCm: _number(json, 'hipCm'),
      bodyFatPercent: _number(json, 'bodyFatPercent'),
      note: note as String?,
      weightChangeKg: _number(json, 'weightChangeKg'),
      waistChangeCm: _number(json, 'waistChangeCm'),
    );
  }
}

/// GET /api/me/measurements: danışanın kendi ölçüm geçmişi (FR-C4).
Future<MeasurementHistory> loadClientMeasurements(SessionApi session) async {
  final json = await session.get('/api/me/measurements');
  try {
    if (json is! Map<String, dynamic>) throw const FormatException();
    return MeasurementHistory.fromJson(json);
  } on FormatException {
    throw const ApiException('Ölçüm geçmişi okunamadı.');
  }
}

double? _number(Map<String, dynamic> json, String key) {
  final value = json[key];
  if (value == null) return null;
  if (value is num && value.isFinite) return value.toDouble();
  throw const FormatException();
}
