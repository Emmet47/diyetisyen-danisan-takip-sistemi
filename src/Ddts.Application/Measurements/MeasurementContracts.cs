namespace Ddts.Application.Measurements;

/// <summary>Ölçüm ekleme ve düzeltme girdisi (FR-C1). Kilo, boy ve bel zorunlu; kalça ve yağ oranı isteğe bağlı.</summary>
public sealed record MeasurementCommand(
    DateOnly MeasuredOn,
    decimal WeightKg,
    decimal HeightCm,
    decimal WaistCm,
    decimal? HipCm,
    decimal? BodyFatPercent,
    string? Note);

/// <summary>
/// Geçmişteki bir ölçüm ve bir önceki ölçüme göre farkları (FR-C2), ör. 72,4 → 71,6 kg ise WeightChangeKg = −0,8.
/// İlk ölçümde ya da değer iki ölçümden birinde yoksa fark null.
/// </summary>
public sealed record MeasurementDto(
    Guid Id,
    DateOnly MeasuredOn,
    decimal WeightKg,
    decimal HeightCm,
    decimal WaistCm,
    decimal? HipCm,
    decimal? BodyFatPercent,
    string? Note,
    decimal? WeightChangeKg,
    decimal? WaistChangeCm,
    decimal? HipChangeCm,
    decimal? BodyFatChangePercent);

/// <summary>
/// Ölçüm geçmişi ve grafik verisi (FR-C2, FR-C3, FR-C4). Items eskiden yeniye sıralı (grafik bu sırayla çizilir).
/// TargetWeightKg grafikteki hedef çizgisi; RemainingKg son ölçümdeki kilo ile hedef arasındaki fark
/// (mutlak değer; yönü danışanın hedef türünden belli). Hedef ya da ölçüm yoksa ikisi de null.
/// </summary>
public sealed record MeasurementHistoryDto(
    decimal? TargetWeightKg,
    decimal? RemainingKg,
    IReadOnlyList<MeasurementDto> Items);
