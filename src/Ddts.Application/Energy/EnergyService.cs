using Ddts.Application.Abstractions.Persistence;
using Ddts.Application.Abstractions.Security;
using Ddts.Application.Common;
using Ddts.Domain.Common;
using Ddts.Domain.Energy;

namespace Ddts.Application.Energy;

/// <summary>
/// Strategy deseninin istemcisi (Şekil 4.7). Danışanın verisini toplar, hesabı EnergyCalculator'a bırakır;
/// hangi formülün çalışacağını bilmez. Sonuçlar tahminidir (FR-H3, Disclaimer).
/// </summary>
public sealed class EnergyService(
    IClientRepository clients,
    IMeasurementRepository measurements,
    EnergyCalculator calculator,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : IEnergyService
{
    public async Task<EnergyCalculationDto> CalculateForClientAsync(
        Guid clientId, string formula, decimal activityFactor, int adjustmentKcal, CancellationToken cancellationToken = default)
    {
        var dietitianId = currentUser.DietitianId
            ?? throw new InvalidOperationException("Bu işlem yalnızca diyetisyen hesabıyla yapılabilir.");

        var client = await clients.GetAsync(dietitianId, clientId, cancellationToken)
            ?? throw new NotFoundException("Danışan bulunamadı.");

        var history = await measurements.ListAsync(client.Id, DateOnly.MinValue, DateOnly.MaxValue, cancellationToken);
        var latest = history.OrderBy(m => m.MeasuredOn).LastOrDefault()
            ?? throw new DomainException("Enerji hesabı için önce danışanın kilo ve boy ölçümü girilmeli.");

        var input = new EnergyInput(client.Sex, client.AgeOn(LocalTime.Today(timeProvider)), latest.WeightKg, latest.HeightCm);
        var result = calculator.Calculate(formula, input, activityFactor, adjustmentKcal);

        return new EnergyCalculationDto(
            result.Formula,
            new EnergyInputDto(input.Sex, input.AgeYears, input.WeightKg, input.HeightCm),
            result.BmrKcal,
            result.DailyNeedKcal,
            result.TargetKcal,
            result.Disclaimer);
    }
}
