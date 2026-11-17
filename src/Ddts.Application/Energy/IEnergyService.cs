namespace Ddts.Application.Energy;

/// <summary>Gelen port: enerji ihtiyacı hesabı (H modülü, FR-H1, Şekil 4.7).</summary>
public interface IEnergyService
{
    /// <summary>
    /// Danışanın son ölçümü ve yaşıyla seçilen formüle göre BMH, günlük ihtiyaç ve hedef enerjiyi hesaplar.
    /// formula: "MifflinStJeor" ya da "HarrisBenedict"; activityFactor 1,2–1,9; adjustmentKcal ör. −500.
    /// </summary>
    Task<EnergyCalculationDto> CalculateForClientAsync(
        Guid clientId, string formula, decimal activityFactor, int adjustmentKcal, CancellationToken cancellationToken = default);
}
