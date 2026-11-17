// Tarih, saat ve sayı biçimlendirme yardımcıları.
// Türkiye saati UTC+3 (yaz saati yok). Randevu zamanları API'de UTC gelir;
// ekranda telefonun saat dilimine bakmadan Türkiye saatine çevrilir.
// Öğün saatleri zaten Türkiye saatidir ("08:00:00").

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

const turkeyOffset = Duration(hours: 3);

/// UTC zamanı Türkiye saatine çevirir (dönen değerin gün/saat alanları
/// Türkiye saatini gösterir).
DateTime turkeyTime(DateTime value) => value.toUtc().add(turkeyOffset);

/// Türkiye saatine göre bugünün tarihi (saat kısmı sıfır).
DateTime turkeyToday() {
  final now = turkeyTime(DateTime.now());
  return DateTime.utc(now.year, now.month, now.day);
}

/// API'nin beklediği "yyyy-aa-gg" tarih metni.
String dateKey(DateTime date) =>
    '${date.year.toString().padLeft(4, '0')}-'
    '${date.month.toString().padLeft(2, '0')}-'
    '${date.day.toString().padLeft(2, '0')}';

/// "yyyy-aa-gg" metnini tarihe çevirir.
DateTime parseDate(String value) {
  final parts = value.split('-').map(int.parse).toList();
  return DateTime.utc(parts[0], parts[1], parts[2]);
}

String dayLabel(DateTime date) => '${date.day} ${months[date.month - 1]}';

String timeLabel(DateTime date) =>
    '${date.hour.toString().padLeft(2, '0')}:${date.minute.toString().padLeft(2, '0')}';

/// "08:00:00" → "08.00"
String mealTimeLabel(String time) =>
    time.length >= 5 ? time.substring(0, 5).replaceAll(':', '.') : time;

/// 1.234 biçiminde tam sayı enerji etiketi.
String energyLabel(num value) {
  final digits = value.round().abs().toString();
  final grouped = digits.replaceAllMapped(
    RegExp(r'\B(?=(\d{3})+(?!\d))'),
    (_) => '.',
  );
  return value.round() < 0 ? '−$grouped' : grouped;
}

/// 2,5 biçiminde ondalık etiket.
String quantityLabel(num value) => value
    .toStringAsFixed(2)
    .replaceFirst(RegExp(r'\.?0+$'), '')
    .replaceAll('.', ',');
