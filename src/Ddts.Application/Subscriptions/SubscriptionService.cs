using System.Text.Json;
using Ddts.Application.Abstractions.Auditing;
using Ddts.Application.Abstractions.Payments;
using Ddts.Application.Abstractions.Persistence;
using Ddts.Application.Abstractions.Security;
using Ddts.Application.Common;
using Ddts.Domain.Common;
using Ddts.Domain.Subscriptions;

namespace Ddts.Application.Subscriptions;

/// <summary>Abonelik durumu, paket değişimi ve ödeme simülasyonu (FR-L2–L4, BR-02).</summary>
public sealed class SubscriptionService(
    ISubscriptionRepository subscriptions,
    ISubscriptionPlanRepository subscriptionPlans,
    IClientRepository clients,
    IUnitOfWork unitOfWork,
    IPaymentGateway paymentGateway,
    IAuditLogger auditLogger,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : ISubscriptionService
{
    public async Task<IReadOnlyList<SubscriptionPlanDto>> ListPlansAsync(CancellationToken cancellationToken = default)
    {
        var plans = await subscriptionPlans.ListActiveAsync(cancellationToken);
        return plans.Select(SubscriptionPlanDto.From).ToList();
    }

    public async Task<SubscriptionStatusDto> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var dietitianId = CurrentDietitianId();
        var subscription = await subscriptions.GetCurrentAsync(dietitianId, cancellationToken)
            ?? throw new NotFoundException("Abonelik bulunamadı.");

        return await ToStatusAsync(subscription, dietitianId, cancellationToken);
    }

    public async Task<SubscriptionStatusDto> ChangePlanAsync(Guid subscriptionPlanId, CancellationToken cancellationToken = default)
    {
        var dietitianId = CurrentDietitianId();
        Subscription? newSubscription = null;

        await unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var newPlan = await subscriptionPlans.GetAsync(subscriptionPlanId, cancellationToken);
            if (newPlan is null || !newPlan.IsActive)
            {
                throw new DomainException("Seçilen paket satışta değil.");
            }

            // Satır kilitlenir: aynı anda danışan eklenirse o istek bu işlemin bitmesini bekler.
            var current = await subscriptions.GetActiveForUpdateAsync(dietitianId, cancellationToken);
            if (current?.SubscriptionPlanId == newPlan.Id)
            {
                throw new DomainException("Zaten bu paketi kullanıyorsun.");
            }

            // BR-02: düşürmede aktif danışan sayısı yeni limiti aşamaz
            var activeClients = await clients.CountActiveAsync(dietitianId, cancellationToken);
            if (activeClients > newPlan.MaxClients)
            {
                throw new SubscriptionLimitException(
                    $"{newPlan.Name} paketinde en fazla {newPlan.MaxClients} aktif danışan olabilir, sende {activeClients} var. " +
                    "Önce bazı danışanları pasife alabilirsin.");
            }

            var payment = await paymentGateway.ChargeAsync(dietitianId, newPlan.MonthlyPrice, cancellationToken);
            if (!payment.Succeeded)
            {
                throw new DomainException(payment.Error ?? "Ödeme alınamadı.");
            }

            // İki adım: önce eski abonelik iptal edilip kaydedilir, sonra yenisi eklenir.
            // Böylece "tek aktif abonelik" tekil indeksi hiçbir an iki aktif satır görmez.
            if (current is not null)
            {
                current.Cancel();
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }

            newSubscription = new Subscription(dietitianId, newPlan, Today());
            subscriptions.Add(newSubscription);
            await auditLogger.LogAsync(
                "SubscriptionChanged",
                "Subscription",
                newSubscription.Id,
                JsonSerializer.Serialize(new { from = current?.Plan.Name, to = newPlan.Name }),
                cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }, cancellationToken);

        return await ToStatusAsync(newSubscription!, dietitianId, cancellationToken);
    }

    public async Task<SubscriptionStatusDto> RenewAsync(CancellationToken cancellationToken = default)
    {
        var dietitianId = CurrentDietitianId();
        var subscription = await subscriptions.GetCurrentAsync(dietitianId, cancellationToken)
            ?? throw new NotFoundException("Abonelik bulunamadı.");

        if (subscription.Status == SubscriptionStatus.Cancelled)
        {
            throw new DomainException("İptal edilmiş abonelik yenilenemez, yeni bir paket seç.");
        }

        var payment = await paymentGateway.ChargeAsync(dietitianId, subscription.Plan.MonthlyPrice, cancellationToken);
        if (!payment.Succeeded)
        {
            throw new DomainException(payment.Error ?? "Ödeme alınamadı.");
        }

        subscription.Renew();
        await auditLogger.LogAsync("SubscriptionRenewed", "Subscription", subscription.Id, null, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return await ToStatusAsync(subscription, dietitianId, cancellationToken);
    }

    private async Task<SubscriptionStatusDto> ToStatusAsync(Subscription subscription, Guid dietitianId, CancellationToken cancellationToken)
    {
        var activeClients = await clients.CountActiveAsync(dietitianId, cancellationToken);
        var usage = new UsageDto(activeClients, subscription.Plan.MaxClients, subscription.IsNearLimit(activeClients));

        return new SubscriptionStatusDto(
            subscription.Id,
            subscription.SubscriptionPlanId,
            subscription.Plan.Name,
            subscription.Plan.MonthlyPrice,
            subscription.Status,
            subscription.StartDate,
            subscription.EndDate,
            usage);
    }

    // Uç noktalar yalnızca diyetisyene açık ([Authorize(Roles = "Dietitian")]); kimlik token'dan gelir.
    private Guid CurrentDietitianId() =>
        currentUser.DietitianId ?? throw new InvalidOperationException("Bu işlem yalnızca diyetisyen hesabıyla yapılabilir.");

    private DateOnly Today() => DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
}
