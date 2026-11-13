using Ddts.Domain.Common;
using Ddts.Domain.Foods;

namespace Ddts.Domain.Plans;

/// <summary>
/// Öğündeki besin satırı: Composite'in yaprağı (Şekil 4.5) ve Prototype'ın somut örneği (Şekil 4.8).
/// Alternatif satır (ör. "2 yumurta yerine 2 dilim peynir") aynı öğündeki bir ana satırı gösterir
/// ve toplam hesabına katılmaz (BR-06).
/// </summary>
public sealed class MealItem : INutritionComponent, IPrototype<MealItem>
{
    private MealItem()
    {
    }

    public MealItem(FoodItem foodItem, decimal portionCount, Guid? alternativeToItemId = null)
    {
        Id = Guid.CreateVersion7();
        FoodItem = foodItem;
        FoodItemId = foodItem.Id;
        PortionCount = Guard.InRange(portionCount, 0.01m, 20m, "Porsiyon sayısı");
        AlternativeToItemId = alternativeToItemId;
    }

    // Clone() için: yeni kimlik, aynı besin ve porsiyon.
    private MealItem(MealItem source)
    {
        Id = Guid.CreateVersion7();
        FoodItem = source.FoodItem;
        FoodItemId = source.FoodItemId;
        PortionCount = source.PortionCount;
        AlternativeToItemId = source.AlternativeToItemId;
    }

    public Guid Id { get; private set; }

    public Guid FoodItemId { get; private set; }

    /// <summary>Satırdaki besin. Kopyalanmaz, plan ve şablon aynı besin kaydını paylaşır.</summary>
    public FoodItem FoodItem { get; private set; } = null!;

    public decimal PortionCount { get; private set; }

    /// <summary>Doluysa bu satır, aynı öğündeki o ana satırın alternatifidir.</summary>
    public Guid? AlternativeToItemId { get; private set; }

    /// <summary>Türetilmiş alan (diyagramda /IsAlternative): veritabanında sütunu yok.</summary>
    public bool IsAlternative => AlternativeToItemId is not null;

    /// <summary>Porsiyon sayısı × bir porsiyonun gramı.</summary>
    public decimal Grams() => PortionCount * LoadedFood().PortionGrams;

    public NutrientTotals CalculateTotals() => LoadedFood().NutrientsFor(Grams());

    /// <summary>
    /// Yeni kimlikli kopya. Alternatif bağlantısı hâlâ eski satırı gösterir;
    /// Meal.Clone() bunu kopyadaki yeni satırın kimliğine çevirir.
    /// </summary>
    public MealItem Clone() => new(this);

    internal void PointAlternativeTo(Guid? itemId) => AlternativeToItemId = itemId;

    // Hesap için besin bilgisi gerekir; repository satırları besinleriyle birlikte yükler (Include).
    private FoodItem LoadedFood() =>
        FoodItem ?? throw new InvalidOperationException(
            "MealItem.FoodItem yüklenmemiş. Sorguda besinleri de yükleyin (Include).");
}
