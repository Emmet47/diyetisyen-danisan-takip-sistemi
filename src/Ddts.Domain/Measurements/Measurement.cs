using Ddts.Domain.Common;

namespace Ddts.Domain.Measurements;

/// <summary>
/// Danışanın bir günkü vücut ölçümü (Şekil 4.1, FR-C1). Kilo, boy ve bel zorunlu; kalça ve yağ oranı
/// isteğe bağlı. Aynı danışana aynı gün için ikinci ölçüm girilemez (veritabanında tekil kısıt).
/// </summary>
public sealed class Measurement
{
    private Measurement()
    {
    }

    public Measurement(
        Guid clientId,
        DateOnly measuredOn,
        decimal weightKg,
        decimal heightCm,
        decimal waistCm,
        decimal? hipCm,
        decimal? bodyFatPercent,
        string? note)
    {
        Id = Guid.CreateVersion7();
        ClientId = clientId;
        MeasuredOn = measuredOn;
        WeightKg = Guard.InRange(weightKg, 20m, 350m, "Kilo (kg)");
        HeightCm = Guard.InRange(heightCm, 100m, 250m, "Boy (cm)");
        WaistCm = Guard.InRange(waistCm, 30m, 250m, "Bel çevresi (cm)");
        HipCm = Guard.InRange(hipCm, 30m, 250m, "Kalça çevresi (cm)");
        BodyFatPercent = Guard.InRange(bodyFatPercent, 2m, 75m, "Vücut yağ oranı (%)");
        Note = Guard.Optional(note, 300, "Not");
    }

    public Guid Id { get; private set; }

    public Guid ClientId { get; private set; }

    public DateOnly MeasuredOn { get; private set; }

    public decimal WeightKg { get; private set; }

    public decimal HeightCm { get; private set; }

    public decimal WaistCm { get; private set; }

    public decimal? HipCm { get; private set; }

    public decimal? BodyFatPercent { get; private set; }

    public string? Note { get; private set; }
}
