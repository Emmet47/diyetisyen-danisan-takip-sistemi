using Ddts.Application.Notifications;
using Ddts.Application.Subscriptions;

namespace Ddts.Api.Workers;

/// <summary>
/// Zamanlanmış iş (sürücü adapter, Şekil 7.1, 5.7): açılışta ve her 10 dakikada bir çalışır. Süresi dolan
/// abonelikleri "Expired" yapar (FR-L4) ve zamanı gelen hatırlatmaları üretir (ReminderService, FR-J1).
/// Bir adımda hata olursa loglanır, iş bir sonraki turda yeniden dener.
/// </summary>
public sealed class ReminderWorker(IServiceScopeFactory scopeFactory, ILogger<ReminderWorker> logger) : BackgroundService
{
    public static readonly TimeSpan Interval = TimeSpan.FromMinutes(10);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);

        do
        {
            await RunOnceAsync(stoppingToken);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunOnceAsync(CancellationToken stoppingToken)
    {
        // İki iş birbirinden bağımsız: biri hata verirse diğeri yine çalışır.
        await RunStepAsync("Süresi dolan abonelikler", async services =>
        {
            var expired = await services.GetRequiredService<ISubscriptionService>().ExpireDueAsync(stoppingToken);
            if (expired > 0)
            {
                logger.LogInformation("{Count} aboneliğin süresi doldu.", expired);
            }
        }, stoppingToken);

        // Randevu (24 saat önce), öğün (20.00'den sonra) ve ölçüm (14 gün) hatırlatmaları (FR-J1)
        await RunStepAsync("Hatırlatmalar", services =>
        {
            var now = services.GetRequiredService<TimeProvider>().GetUtcNow().UtcDateTime;
            return services.GetRequiredService<IReminderService>().SendDueRemindersAsync(now, stoppingToken);
        }, stoppingToken);
    }

    private async Task RunStepAsync(string name, Func<IServiceProvider, Task> step, CancellationToken stoppingToken)
    {
        try
        {
            // Servisler istek başına (scoped) kayıtlı: her adım kendi DbContext'iyle çalışır.
            await using var scope = scopeFactory.CreateAsyncScope();
            await step(scope.ServiceProvider);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Uygulama kapanıyor
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "ReminderWorker adımı başarısız oldu ({Step}); 10 dakika sonra yeniden denenecek.", name);
        }
    }
}
