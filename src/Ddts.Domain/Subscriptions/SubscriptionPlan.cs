using Ddts.Domain.Common;

namespace Ddts.Domain.Subscriptions;

/// <summary>
/// Abonelik paketi (Şekil 4.1, FR-L1). Demo paketleri: Başlangıç (10 danışan), Standart (30),
/// Profesyonel (100). IsActive = false ise paket satıştan kalkar ama mevcut abonelikler sürer.
/// </summary>
public sealed class SubscriptionPlan
{
    private SubscriptionPlan()
    {
    }

    public SubscriptionPlan(string name, int maxClients, decimal monthlyPrice)
    {
        Id = Guid.CreateVersion7();
        Apply(name, maxClients, monthlyPrice);
        IsActive = true;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = null!;

    /// <summary>Aynı anda en fazla kaç aktif danışan.</summary>
    public int MaxClients { get; private set; }

    /// <summary>Aylık ücret (TL). Ödeme simülasyonla yapılır (FR-L4).</summary>
    public decimal MonthlyPrice { get; private set; }

    /// <summary>Satışta mı?</summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Yönetici paketi düzenler ya da satıştan kaldırır (FR-L1). Paketi kullanan diyetisyenlerin
    /// aboneliği sürer; yeni limit onların da bir sonraki danışan eklemesinde geçerli olur.
    /// </summary>
    public void Update(string name, int maxClients, decimal monthlyPrice, bool isActive)
    {
        Apply(name, maxClients, monthlyPrice);
        IsActive = isActive;
    }

    private void Apply(string name, int maxClients, decimal monthlyPrice)
    {
        if (maxClients <= 0)
        {
            throw new DomainException("Danışan limiti 0'dan büyük olmalı.");
        }

        Name = Guard.Required(name, 50, "Paket adı");
        MaxClients = maxClients;
        MonthlyPrice = Guard.InRange(monthlyPrice, 0m, 999999.99m, "Aylık ücret");
    }
}
