using Ddts.Application.Measurements;

namespace Ddts.Application.Reports;

/// <summary>
/// Dönemsel ilerleme raporu (FR-K1–K3, Şekil 5.6). Panel (grafikleri web çizer) ve PDF aynı veriyi kullanır.
/// Plan uyumu = (Yedim + 0,5 × Kısmen yedim) ÷ planlı öğün × 100; kayıt sıklığı = kayıtlı gün ÷ gün × 100
/// (FR-K3). Planlı öğün yoksa uyum null.
/// </summary>
public sealed record ProgressReportDto(
    Guid ClientId,
    string ClientName,
    DateOnly From,
    DateOnly To,
    DateTime GeneratedAt,
    IReadOnlyList<MeasurementDto> Measurements,
    decimal? TargetWeightKg,
    decimal? RemainingKg,
    decimal? WeightChangeKg,
    decimal? WaistChangeCm,
    int PlannedMeals,
    int EatenMeals,
    int PartialMeals,
    int SkippedMeals,
    decimal? AdherencePercent,
    int LoggedDays,
    int DayCount,
    decimal LogFrequencyPercent,
    string Disclaimer);

/// <summary>PDF dosyası ve indirme adı (adda kişisel veri yok).</summary>
public sealed record ReportFileDto(byte[] Content, string FileName);
