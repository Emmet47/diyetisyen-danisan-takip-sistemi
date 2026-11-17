using Ddts.Application.Abstractions.Persistence;
using Ddts.Application.Abstractions.Reports;
using Ddts.Application.Abstractions.Security;
using Ddts.Application.Common;
using Ddts.Application.MealLogs;
using Ddts.Application.Measurements;
using Ddts.Domain.Common;
using Ddts.Domain.Energy;
using Ddts.Domain.MealLogs;

namespace Ddts.Application.Reports;

/// <summary>
/// İlerleme raporu (K modülü, Şekil 5.6): dönemin ölçümlerini ve öğün kayıtlarını okuyup farkları, hedefe kalan
/// kiloyu, plan uyumunu ve kayıt sıklığını hesaplar. Grafikleri web çizer; PDF'i IReportPdfRenderer üretir.
/// </summary>
public sealed class ReportService(
    IClientRepository clients,
    IMeasurementRepository measurements,
    IMealLogRepository mealLogs,
    INutritionPlanRepository plans,
    IReportPdfRenderer pdfRenderer,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : IReportService
{
    public const int DefaultDays = 30;
    public const int MaxDays = 366;

    public async Task<ProgressReportDto> GetProgressAsync(
        Guid clientId, DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default)
    {
        var dietitianId = currentUser.DietitianId
            ?? throw new InvalidOperationException("Bu işlem yalnızca diyetisyen hesabıyla yapılabilir.");
        var client = await clients.GetAsync(dietitianId, clientId, cancellationToken)
            ?? throw new NotFoundException("Danışan bulunamadı.");

        var end = to ?? LocalTime.Today(timeProvider);
        var start = from ?? end.AddDays(-(DefaultDays - 1));
        var dayCount = end.DayNumber - start.DayNumber + 1;

        if (start > end)
        {
            throw new DomainException("Başlangıç tarihi bitiş tarihinden sonra olamaz.");
        }

        if (dayCount > MaxDays)
        {
            throw new DomainException($"En fazla {MaxDays} günlük dönem seçilebilir.");
        }

        // Farklar dönem öncesindeki son ölçüme göre de hesaplanır; listede yalnızca dönemin ölçümleri kalır.
        var history = MeasurementTrend.Build(
            await measurements.ListAsync(client.Id, DateOnly.MinValue, end, cancellationToken));
        var inPeriod = history.Where(m => m.MeasuredOn >= start).ToList();
        var latest = history.Count == 0 ? null : history[^1];

        var clientPlans = await plans.ListForClientAsync(client.Id, cancellationToken);
        var planned = PlannedMeals.Between(clientPlans, start, end).Sum(day => day.Meals.Count);
        var logs = await mealLogs.ListAsync(client.Id, start, end, cancellationToken);

        var eaten = logs.Count(l => l.Status == MealLogStatus.Eaten);
        var partial = logs.Count(l => l.Status == MealLogStatus.Partial);
        var skipped = logs.Count(l => l.Status == MealLogStatus.Skipped);
        var loggedDays = logs.Select(l => l.LogDate).Distinct().Count();

        return new ProgressReportDto(
            client.Id,
            client.FullName,
            start,
            end,
            timeProvider.GetUtcNow().UtcDateTime,
            inPeriod,
            client.TargetWeightKg,
            MeasurementTrend.RemainingKg(client.TargetWeightKg, latest?.WeightKg),
            inPeriod.Count < 2 ? null : inPeriod[^1].WeightKg - inPeriod[0].WeightKg,
            inPeriod.Count < 2 ? null : inPeriod[^1].WaistCm - inPeriod[0].WaistCm,
            planned,
            eaten,
            partial,
            skipped,
            AdherencePercent(eaten, partial, planned),
            loggedDays,
            dayCount,
            Math.Round(loggedDays * 100m / dayCount, 1, MidpointRounding.AwayFromZero),
            EnergyResult.DefaultDisclaimer);
    }

    public async Task<ReportFileDto> RenderPdfAsync(
        Guid clientId, DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default)
    {
        var report = await GetProgressAsync(clientId, from, to, cancellationToken);
        var content = pdfRenderer.Render(report);
        return new ReportFileDto(content, $"ilerleme-raporu-{report.From:yyyyMMdd}-{report.To:yyyyMMdd}.pdf");
    }

    /// <summary>FR-K3: (Yedim + 0,5 × Kısmen yedim) ÷ planlı öğün × 100, ör. (8 + 0,5 × 2) ÷ 14 = %64,3 (UT-13).</summary>
    public static decimal? AdherencePercent(int eaten, int partial, int planned) =>
        planned == 0 ? null : Math.Round((eaten + 0.5m * partial) * 100m / planned, 1, MidpointRounding.AwayFromZero);
}
