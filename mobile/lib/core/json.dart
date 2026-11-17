// API yanıtlarını okurken kullanılan küçük yardımcılar. Beklenen alan yoksa ya
// da tipi yanlışsa FormatException atılır; [readResponse] bunu Türkçe mesaja
// çevirir.
import 'api_client.dart';

Map<String, dynamic> asMap(Object? value) {
  if (value is Map<String, dynamic>) return value;
  throw const FormatException();
}

List<Object?> asList(Object? value) {
  if (value is List) return value;
  throw const FormatException();
}

String readString(Map<String, dynamic> json, String key) {
  final value = json[key];
  if (value is String) return value;
  throw const FormatException();
}

String? readOptionalString(Map<String, dynamic> json, String key) {
  final value = json[key];
  if (value == null) return null;
  if (value is String) return value;
  throw const FormatException();
}

double readNumber(Map<String, dynamic> json, String key) =>
    readOptionalNumber(json, key) ?? (throw const FormatException());

double? readOptionalNumber(Map<String, dynamic> json, String key) {
  final value = json[key];
  if (value == null) return null;
  if (value is num && value.isFinite) return value.toDouble();
  throw const FormatException();
}

bool readBool(Map<String, dynamic> json, String key) {
  final value = json[key];
  if (value is bool) return value;
  throw const FormatException();
}

DateTime readDateTime(Map<String, dynamic> json, String key) =>
    DateTime.tryParse(readString(json, key))?.toUtc() ??
    (throw const FormatException());

/// Yanıtı [read] ile modele çevirir; biçim bozuksa anlaşılır hata verir.
T readResponse<T>(Object? json, T Function(Object? json) read) {
  try {
    return read(json);
  } on FormatException {
    throw const ApiException('Sunucudan beklenmeyen bir yanıt geldi.');
  }
}
