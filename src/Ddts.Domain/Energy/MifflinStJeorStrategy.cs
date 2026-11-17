using Ddts.Domain.Clients;

namespace Ddts.Domain.Energy;

/// <summary>
/// Mifflin-St Jeor formülü. Erkek: 10·kg + 6,25·cm − 5·yaş + 5; kadın: 10·kg + 6,25·cm − 5·yaş − 161.
/// </summary>
public sealed class MifflinStJeorStrategy : IEnergyStrategy
{
    public string Name => "MifflinStJeor";

    public decimal CalculateBmr(EnergyInput input)
    {
        var common = 10m * input.WeightKg + 6.25m * input.HeightCm - 5m * input.AgeYears;
        return input.Sex == Sex.Male ? common + 5m : common - 161m;
    }
}
