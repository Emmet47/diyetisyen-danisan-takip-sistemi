using Ddts.Domain.Common;

namespace Ddts.Domain.Foods;

/// <summary>
/// Yerel besin kaydı (Şekil 4.2, FR-D1–D3). Değerler 100 g içindir; porsiyonun gram karşılığı ayrıca tutulur.
/// DietitianId boşsa herkesin kullandığı ortak kayıttır, doluysa o diyetisyenin kendi kaydıdır.
/// USDA'dan içe aktarılan besinde kaynak adı, dış kimlik (fdcId) ve çekilme zamanı dolu olur.
/// </summary>
public sealed class FoodItem
{
    private FoodItem()
    {
    }

    public FoodItem(
        Guid? dietitianId,
        string name,
        string portionName,
        decimal portionGrams,
        decimal energyKcalPer100g,
        decimal proteinPer100g,
        decimal carbsPer100g,
        decimal fatPer100g,
        string sourceName,
        string? externalId,
        DateTime? sourceUpdatedAt)
    {
        var cleanExternalId = Guard.Optional(externalId, 50, "Dış kaynak kimliği");
        if ((cleanExternalId is null) != (sourceUpdatedAt is null))
        {
            throw new DomainException("Dış kaynaktan gelen besinde kimlik ve çekilme zamanı birlikte girilmeli.");
        }

        Id = Guid.CreateVersion7();
        DietitianId = dietitianId;
        Apply(name, portionName, portionGrams, energyKcalPer100g, proteinPer100g, carbsPer100g, fatPer100g);
        SourceName = Guard.Required(sourceName, 50, "Kaynak adı");
        ExternalId = cleanExternalId;
        SourceUpdatedAt = sourceUpdatedAt is null ? null : Guard.Utc(sourceUpdatedAt.Value, nameof(sourceUpdatedAt));
    }

    public Guid Id { get; private set; }

    /// <summary>Sahibi olan diyetisyen; null ise ortak kayıt (kimse düzenleyemez, FR-D2).</summary>
    public Guid? DietitianId { get; private set; }

    public string Name { get; private set; } = null!;

    /// <summary>"1 dilim", "1 su bardağı" gibi.</summary>
    public string PortionName { get; private set; } = null!;

    /// <summary>Bir porsiyonun gram karşılığı.</summary>
    public decimal PortionGrams { get; private set; }

    public decimal EnergyKcalPer100g { get; private set; }

    public decimal ProteinPer100g { get; private set; }

    public decimal CarbsPer100g { get; private set; }

    public decimal FatPer100g { get; private set; }

    /// <summary>Veri kaynağı: "Yerel" ya da "USDA FoodData Central".</summary>
    public string SourceName { get; private set; } = null!;

    /// <summary>Dış kaynaktaki kimlik (USDA fdcId).</summary>
    public string? ExternalId { get; private set; }

    /// <summary>Dış kaynaktan çekildiği an (güncellik bilgisi).</summary>
    public DateTime? SourceUpdatedAt { get; private set; }

    /// <summary>Verilen gram için enerji ve makrolar (Composite'in yaprağı MealItem bunu çağırır).</summary>
    /// <summary>
    /// Diyetisyenin kendi besin kaydını düzeltir (FR-D2). Kaynak bilgisi (Yerel / USDA, dış kimlik) değişmez.
    /// Kaydın kime ait olduğu (ortak kayıtlar düzenlenemez) FoodService'te denetlenir.
    /// </summary>
    public void Update(
        string name,
        string portionName,
        decimal portionGrams,
        decimal energyKcalPer100g,
        decimal proteinPer100g,
        decimal carbsPer100g,
        decimal fatPer100g) =>
        Apply(name, portionName, portionGrams, energyKcalPer100g, proteinPer100g, carbsPer100g, fatPer100g);

    public NutrientTotals NutrientsFor(decimal grams)
    {
        var ratio = grams / 100m;
        return new NutrientTotals(
            EnergyKcalPer100g * ratio,
            ProteinPer100g * ratio,
            CarbsPer100g * ratio,
            FatPer100g * ratio);
    }

    // Önce hepsi denetlenir, sonra atanır: hatalı bir değer kaydı yarı değişmiş bırakmaz.
    private void Apply(
        string name,
        string portionName,
        decimal portionGrams,
        decimal energyKcalPer100g,
        decimal proteinPer100g,
        decimal carbsPer100g,
        decimal fatPer100g)
    {
        if (proteinPer100g + carbsPer100g + fatPer100g > 100m)
        {
            throw new DomainException("100 gramdaki protein, karbonhidrat ve yağ toplamı 100 g'ı geçemez.");
        }

        var cleanName = Guard.Required(name, 150, "Besin adı");
        var cleanPortionName = Guard.Required(portionName, 50, "Porsiyon adı");
        var grams = Guard.InRange(portionGrams, 0.1m, 99999.9m, "Porsiyon (g)");
        var energy = Guard.InRange(energyKcalPer100g, 0m, 900m, "Enerji (kcal/100 g)");
        var protein = Guard.InRange(proteinPer100g, 0m, 100m, "Protein (g/100 g)");
        var carbs = Guard.InRange(carbsPer100g, 0m, 100m, "Karbonhidrat (g/100 g)");
        var fat = Guard.InRange(fatPer100g, 0m, 100m, "Yağ (g/100 g)");

        Name = cleanName;
        PortionName = cleanPortionName;
        PortionGrams = grams;
        EnergyKcalPer100g = energy;
        ProteinPer100g = protein;
        CarbsPer100g = carbs;
        FatPer100g = fat;
    }
}
