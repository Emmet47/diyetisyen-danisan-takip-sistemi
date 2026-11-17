using Ddts.Domain.Common;

namespace Ddts.Domain.Energy;

/// <summary>
/// Strategy deseninin bağlamı (Context, Şekil 4.7). Stratejiler DI ile gelir; istekteki formül adına göre
/// biri seçilir. Günlük ihtiyaç = BMH × aktivite katsayısı (1,2–1,9); hedef = günlük ihtiyaç + düzeltme.
/// </summary>
public sealed class EnergyCalculator(IEnumerable<IEnergyStrategy> strategies)
{
    public const decimal MinActivityFactor = 1.2m;
    public const decimal MaxActivityFactor = 1.9m;
    public const int MaxAdjustmentKcal = 1500;

    private readonly IReadOnlyList<IEnergyStrategy> _strategies = strategies.ToList();

    public EnergyResult Calculate(string formula, EnergyInput input, decimal activityFactor, int adjustmentKcal)
    {
        var strategy = _strategies.FirstOrDefault(s => string.Equals(s.Name, formula?.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? throw new DomainException(
                $"Bilinmeyen formül. Seçenekler: {string.Join(", ", _strategies.Select(s => s.Name))}.");

        Guard.InRange(activityFactor, MinActivityFactor, MaxActivityFactor, "Aktivite katsayısı");

        if (Math.Abs(adjustmentKcal) > MaxAdjustmentKcal)
        {
            throw new DomainException($"Kcal düzeltmesi -{MaxAdjustmentKcal} ile +{MaxAdjustmentKcal} arasında olmalı.");
        }

        var bmr = Math.Round(strategy.CalculateBmr(input), 2, MidpointRounding.AwayFromZero);
        var dailyNeed = Math.Round(bmr * activityFactor, 2, MidpointRounding.AwayFromZero);
        var target = dailyNeed + adjustmentKcal;

        if (bmr <= 0m || target <= 0m)
        {
            throw new DomainException("Girilen değerlerle anlamlı bir enerji ihtiyacı hesaplanamadı.");
        }

        return new EnergyResult(strategy.Name, bmr, dailyNeed, target, EnergyResult.DefaultDisclaimer);
    }
}
