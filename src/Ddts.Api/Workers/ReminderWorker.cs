using Ddts.Application.Subscriptions;

namespace Ddts.Api.Workers;

/// <summary>
/// Zamanlanmış iş (sürücü adapter, Şekil 7.1): her 10 dakikada bir çalışır. Şimdilik süresi dolan
/// abonelikleri "Expired" yapar (FR-L4); hatırlatma bildirimleri J modülüyle (ReminderService) eklenecek.
/// Bir çalıştırmada hata olursa loglanır, iş bir sonraki turda yeniden dener.
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
        try
        {
            // Servisler istek başına (scoped) kayıtlı: her tur kendi DbContext'iyle çalışır.
            await using var scope = scopeFactory.CreateAsyncScope();
            var subscriptionService = scope.ServiceProvider.GetRequiredService<ISubscriptionService>();

            var expired = await subscriptionService.ExpireDueAsync(stoppingToken);
            if (expired > 0)
            {
                logger.LogInformation("{Count} aboneliğin süresi doldu.", expired);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Uygulama kapanıyor
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "ReminderWorker turu başarısız oldu; 10 dakika sonra yeniden denenecek.");
        }
    }
}
