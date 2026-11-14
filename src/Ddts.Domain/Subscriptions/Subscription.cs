using Ddts.Domain.Common;

namespace Ddts.Domain.Subscriptions;

/// <summary>
/// Diyetisyenin aylık aboneliği (Şekil 4.1, FR-L2–L4). Bir diyetisyenin aynı anda tek aktif
/// aboneliği olur (veritabanında kısmi tekil indeks). Bitiş tarihi dahil: o gün de aktiftir.
/// </summary>
public sealed class Subscription
{
    private Subscription()
    {
    }

    public Subscription(Guid dietitianId, SubscriptionPlan plan, DateOnly startDate)
    {
        Id = Guid.CreateVersion7();
        DietitianId = dietitianId;
        Plan = plan;
        SubscriptionPlanId = plan.Id;
        Status = SubscriptionStatus.Active;
        StartDate = startDate;
        EndDate = startDate.AddMonths(1);
    }

    public Guid Id { get; private set; }

    public Guid DietitianId { get; private set; }

    public Guid SubscriptionPlanId { get; private set; }

    /// <summary>Paket bilgisi (danışan limiti için gerekli; repository paketle birlikte yükler).</summary>
    public SubscriptionPlan Plan { get; private set; } = null!;

    public SubscriptionStatus Status { get; private set; }

    public DateOnly StartDate { get; private set; }

    public DateOnly EndDate { get; private set; }

    public bool IsActiveOn(DateOnly today) =>
        Status == SubscriptionStatus.Active && StartDate <= today && today <= EndDate;

    /// <summary>
    /// Yeni danışan eklenebilir mi? Abonelik aktif değilse ya da aktif danışan sayısı limite
    /// ulaştıysa SubscriptionLimitException (API'de 409). Pasif danışanlar sayılmaz (BR-01).
    /// </summary>
    public void EnsureCanAddClient(int activeClients)
    {
        if (Status != SubscriptionStatus.Active)
        {
            throw new SubscriptionLimitException("Aboneliğin aktif değil. Yeni danışan eklemek için aboneliğini yenile.");
        }

        var plan = LoadedPlan();
        if (activeClients >= plan.MaxClients)
        {
            throw new SubscriptionLimitException(
                $"{plan.Name} paketinde en fazla {plan.MaxClients} aktif danışan olabilir. " +
                "Paketini yükseltebilir ya da bir danışanı pasife alabilirsin.");
        }
    }

    /// <summary>Limitin %80'ine gelindi mi? (panelde sarı uyarı, FR-L2). Ör. 10'luk pakette 8 danışan.</summary>
    public bool IsNearLimit(int activeClients) => activeClients * 5 >= LoadedPlan().MaxClients * 4;

    /// <summary>Bir ay uzatır (ödeme simülasyonundan sonra). İptal edilmiş abonelik yenilenmez.</summary>
    public void Renew()
    {
        if (Status == SubscriptionStatus.Cancelled)
        {
            throw new DomainException("İptal edilmiş abonelik yenilenemez, yeni paket seçin.");
        }

        Status = SubscriptionStatus.Active;
        EndDate = EndDate.AddMonths(1);
    }

    public void Cancel()
    {
        EnsureActive();
        Status = SubscriptionStatus.Cancelled;
    }

    /// <summary>Bitiş tarihi geçince arka plan işi çağırır: mevcut veriler okunur, yeni kayıt eklenemez (FR-L4).</summary>
    public void Expire()
    {
        EnsureActive();
        Status = SubscriptionStatus.Expired;
    }

    private void EnsureActive()
    {
        if (Status != SubscriptionStatus.Active)
        {
            throw new DomainException("Yalnızca aktif abonelik iptal edilebilir ya da sonlandırılabilir.");
        }
    }

    private SubscriptionPlan LoadedPlan() =>
        Plan ?? throw new InvalidOperationException(
            "Subscription.Plan yüklenmemiş. Sorguda paketi de yükleyin (Include).");
}
