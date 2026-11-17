using Ddts.Application.Abstractions.Persistence;
using Ddts.Application.Common;
using Ddts.Application.MealLogs;
using Ddts.Domain.Notifications;

namespace Ddts.Application.Notifications;

/// <summary>
/// Hatırlatmalar (FR-J1, BR-12). Her hatırlatmanın sabit bir tekrar önleme anahtarı (dedupKey) vardır;
/// notifications.dedup_key tekil olduğu için worker kaç kez çalışırsa çalışsın aynı hatırlatma bir kez gider:
/// <list type="bullet">
/// <item>Randevudan 24 saat önce (danışana): <c>appt:{randevuId}:24h</c></item>
/// <item>Saat 20.00'den sonra o gün kaydedilmemiş öğün varsa: <c>meal:{danışanId}:{yyyyMMdd}</c></item>
/// <item>Son ölçümden 14 gün sonra: <c>measure:{danışanId}:{sonÖlçümTarihi}</c></item>
/// </list>
/// </summary>
public sealed class ReminderService(
    IAppointmentRepository appointments,
    INutritionPlanRepository plans,
    IMealLogRepository mealLogs,
    IClientRepository clients,
    IMeasurementRepository measurements,
    INotificationService notifications) : IReminderService
{
    public static readonly TimeSpan AppointmentLead = TimeSpan.FromHours(24);
    public static readonly TimeOnly MealReminderTime = new(20, 0);
    public const int MeasurementIntervalDays = 14;

    public async Task SendDueRemindersAsync(DateTime now, CancellationToken cancellationToken = default)
    {
        var utcNow = LocalTime.RequireUtc(now, nameof(now));

        await AppointmentRemindersAsync(utcNow, cancellationToken);
        await MealRemindersAsync(utcNow, cancellationToken);
        await MeasurementRemindersAsync(utcNow, cancellationToken);
    }

    private async Task AppointmentRemindersAsync(DateTime now, CancellationToken cancellationToken)
    {
        var upcoming = await appointments.ListConfirmedStartingAsync(now, now + AppointmentLead, cancellationToken);

        foreach (var appointment in upcoming)
        {
            var client = await clients.GetAsync(appointment.DietitianId, appointment.ClientId, cancellationToken);
            if (client is null)
            {
                continue;
            }

            await notifications.NotifyAsync(
                client.UserId,
                NotificationType.AppointmentReminder,
                "Randevu hatırlatması",
                $"{LocalTime.Format(appointment.StartsAt)} randevunu unutma.",
                $"appt:{appointment.Id}:24h",
                cancellationToken);
        }

    }

    private async Task MealRemindersAsync(DateTime now, CancellationToken cancellationToken)
    {
        var local = LocalTime.ToLocal(now);
        if (TimeOnly.FromDateTime(local) < MealReminderTime)
        {
            return;
        }

        var today = DateOnly.FromDateTime(local);
        var published = await plans.ListPublishedAsync(cancellationToken);

        foreach (var plan in published)
        {
            var planned = PlannedMeals.For(plan, today);
            if (planned.Count == 0)
            {
                continue;
            }

            var logs = await mealLogs.ListAsync(plan.ClientId, today, today, cancellationToken);
            if (planned.All(meal => logs.Any(l => l.MealId == meal.Id)))
            {
                continue;
            }

            var client = await clients.GetAsync(plan.DietitianId, plan.ClientId, cancellationToken);
            if (client is null || !client.IsActive)
            {
                continue;
            }

            await notifications.NotifyAsync(
                client.UserId,
                NotificationType.MealLogReminder,
                "Öğün kaydı hatırlatması",
                "Bugünkü öğünlerinden kaydetmediklerin var.",
                $"meal:{client.Id}:{today:yyyyMMdd}",
                cancellationToken);
        }

    }

    private async Task MeasurementRemindersAsync(DateTime now, CancellationToken cancellationToken)
    {
        var today = LocalTime.DateOf(now);

        foreach (var client in await clients.ListAllActiveAsync(cancellationToken))
        {
            var history = await measurements.ListAsync(client.Id, DateOnly.MinValue, today, cancellationToken);
            var last = history.Count == 0 ? (DateOnly?)null : history.Max(m => m.MeasuredOn);

            if (last is not { } lastDate || today.DayNumber - lastDate.DayNumber < MeasurementIntervalDays)
            {
                continue;
            }

            await notifications.NotifyAsync(
                client.UserId,
                NotificationType.MeasurementReminder,
                "Ölçüm zamanı",
                "Son ölçümünün üzerinden 14 gün geçti, yeni ölçüm için diyetisyeninle görüşebilirsin.",
                $"measure:{client.Id}:{lastDate:yyyyMMdd}",
                cancellationToken);
        }

    }
}
