using Ddts.Domain.Measurements;

namespace Ddts.Application.Measurements;

/// <summary>
/// Ölçüm geçmişinde her ölçümün bir öncekine göre farkı (FR-C2): yeni − eski, ör. 72,4 → 71,6 kg = −0,8.
/// Ölçüm geçmişi (MeasurementService) ve ilerleme raporu (ReportService) aynı hesabı kullanır.
/// </summary>
public static class MeasurementTrend
{
    /// <summary>Ölçümleri eskiden yeniye dizer, farklarıyla birlikte döner.</summary>
    public static IReadOnlyList<MeasurementDto> Build(IEnumerable<Measurement> measurements)
    {
        var items = new List<MeasurementDto>();
        Measurement? previous = null;

        foreach (var m in measurements.OrderBy(m => m.MeasuredOn))
        {
            items.Add(new MeasurementDto(
                m.Id,
                m.MeasuredOn,
                m.WeightKg,
                m.HeightCm,
                m.WaistCm,
                m.HipCm,
                m.BodyFatPercent,
                m.Note,
                Change(previous?.WeightKg, m.WeightKg),
                Change(previous?.WaistCm, m.WaistCm),
                Change(previous?.HipCm, m.HipCm),
                Change(previous?.BodyFatPercent, m.BodyFatPercent)));
            previous = m;
        }

        return items;
    }

    /// <summary>Hedefe kalan kilo (FR-C3): son kilo ile hedef arasındaki fark (mutlak değer).</summary>
    public static decimal? RemainingKg(decimal? targetWeightKg, decimal? latestWeightKg) =>
        targetWeightKg is { } target && latestWeightKg is { } latest ? Math.Abs(latest - target) : null;

    private static decimal? Change(decimal? previous, decimal? current) =>
        previous is { } p && current is { } c ? c - p : null;
}
