using Ddts.Domain.Clients;
using Ddts.Domain.Common;
using Ddts.Domain.Foods;

namespace Ddts.Domain.Plans;

/// <summary>
/// Danışanın beslenme planı (Şekil 4.2). Composite'in kökü (Şekil 4.5) ve domain event üreten
/// aggregate root (Şekil 4.11). Durumlar: Taslak → Yayımlandı → Arşiv; yalnızca taslak düzenlenir (BR-04).
/// </summary>
public sealed class NutritionPlan : AggregateRoot, INutritionComponent
{
    private readonly List<Meal> _meals = [];

    private NutritionPlan()
    {
    }

    private NutritionPlan(Guid dietitianId, Guid clientId, string title, PlanType planType, Guid? sourceTemplateId)
    {
        Id = Guid.CreateVersion7();
        DietitianId = dietitianId;
        ClientId = clientId;
        Title = Guard.Required(title, 100, "Plan başlığı");
        PlanType = planType;
        SourceTemplateId = sourceTemplateId;
        Status = PlanStatus.Draft;
    }

    public Guid Id { get; private set; }

    public Guid DietitianId { get; private set; }

    public Guid ClientId { get; private set; }

    /// <summary>Şablondan kopyalandıysa şablonun kimliği (Prototype, Şekil 4.8).</summary>
    public Guid? SourceTemplateId { get; private set; }

    public string Title { get; private set; } = null!;

    public PlanType PlanType { get; private set; }

    public PlanStatus Status { get; private set; }

    /// <summary>Hedef günlük enerji (kcal), enerji hesabından gelir (FR-H1).</summary>
    public int? TargetKcal { get; private set; }

    /// <summary>Hedefin hesaplandığı formül (Strategy adı, ör. "MifflinStJeor").</summary>
    public string? EnergyFormula { get; private set; }

    /// <summary>Oluşturulma anı; kayıt sırasında veritabanı yazar (DEFAULT now()).</summary>
    public DateTime CreatedAt { get; private set; }

    public DateTime? PublishedAt { get; private set; }

    public DateTime? ArchivedAt { get; private set; }

    public IReadOnlyCollection<Meal> Meals => _meals.AsReadOnly();

    /// <summary>Danışan için boş taslak plan oluşturur; plan, danışanın diyetisyenine ait olur.</summary>
    public static NutritionPlan CreateDraft(Client client, string title, PlanType type, Guid? sourceTemplateId) =>
        new(client.DietitianId, client.Id, title, type, sourceTemplateId);

    public void AddMeal(Meal meal)
    {
        EnsureDraft();

        if (PlanType == PlanType.Daily && meal.DayNo != 1)
        {
            throw new DomainException("Günlük planda öğünler 1. güne eklenir.");
        }

        _meals.Add(meal);
    }

    /// <summary>
    /// Planı yayımlar ve PlanPublished olayını üretir. Danışanın eski yayımlanmış planını
    /// arşivlemek servis katmanının işi (Şekil 5.4); veritabanındaki kısmi tekil indeks de
    /// aynı danışana iki yayımlanmış plan yazılmasını engeller (BR-03).
    /// </summary>
    public void Publish(DateTime now)
    {
        EnsureDraft();

        if (_meals.Count == 0)
        {
            throw new DomainException("Öğünü olmayan plan yayımlanamaz.");
        }

        Status = PlanStatus.Published;
        PublishedAt = Guard.Utc(now, nameof(now));
        Raise(new PlanPublished(Id, DietitianId, ClientId, now));
    }

    /// <summary>Yayımlanmış planı arşivler. Arşivlenen plan geri açılmaz (BR-04).</summary>
    public void Archive(DateTime now)
    {
        if (Status != PlanStatus.Published)
        {
            throw new DomainException("Yalnızca yayımlanmış plan arşivlenebilir.");
        }

        Status = PlanStatus.Archived;
        ArchivedAt = Guard.Utc(now, nameof(now));
    }

    /// <summary>Plan toplamı = öğün toplamlarının toplamı (haftalık planda 7 günün toplamı).</summary>
    public NutrientTotals CalculateTotals() =>
        _meals.Aggregate(NutrientTotals.Zero, (sum, meal) => sum.Add(meal.CalculateTotals()));

    /// <summary>Bir günün toplamı = o güne ait öğünlerin toplamı.</summary>
    public NutrientTotals CalculateDayTotals(int dayNo) =>
        _meals.Where(m => m.DayNo == dayNo)
            .Aggregate(NutrientTotals.Zero, (sum, meal) => sum.Add(meal.CalculateTotals()));

    /// <summary>Enerji hedefini yazar; NutritionPlanBuilder.WithEnergyTarget kullanır (Şekil 4.6).</summary>
    internal void SetEnergyTarget(int kcal, string formula)
    {
        EnsureDraft();

        if (kcal is < 800 or > 6000)
        {
            throw new DomainException("Hedef enerji 800 ile 6000 kcal arasında olmalı.");
        }

        TargetKcal = kcal;
        EnergyFormula = Guard.Required(formula, 30, "Enerji formülü");
    }

    private void EnsureDraft()
    {
        if (Status != PlanStatus.Draft)
        {
            throw new DomainException("Yalnızca taslak plan değiştirilebilir.");
        }
    }
}
