using Ddts.Domain.Clients;

namespace Ddts.Domain.Energy;

/// <summary>
/// Harris-Benedict formülü (Roza ve Shizgal düzeltmesi).
/// Erkek: 88,362 + 13,397·kg + 4,799·cm − 5,677·yaş; kadın: 447,593 + 9,247·kg + 3,098·cm − 4,330·yaş.
/// </summary>
public sealed class HarrisBenedictStrategy : IEnergyStrategy
{
    public string Name => "HarrisBenedict";

    public decimal CalculateBmr(EnergyInput input) => input.Sex == Sex.Male
        ? 88.362m + 13.397m * input.WeightKg + 4.799m * input.HeightCm - 5.677m * input.AgeYears
        : 447.593m + 9.247m * input.WeightKg + 3.098m * input.HeightCm - 4.330m * input.AgeYears;
}
