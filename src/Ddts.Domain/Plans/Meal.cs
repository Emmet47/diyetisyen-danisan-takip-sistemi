using Ddts.Domain.Common;
using Ddts.Domain.Foods;

namespace Ddts.Domain.Plans;

/// <summary>
/// Öğün (Şekil 4.2): plan ya da şablonun içindeki bir öğün ve besin satırları.
/// Composite'te ara düğüm (Şekil 4.5), Prototype'ta somut örnek (Şekil 4.8).
/// Bir öğün ya bir plana ya da bir şablona aittir; ikisine birden değil (meals tablosunda CHECK).
/// </summary>
public sealed class Meal : INutritionComponent, IPrototype<Meal>
{
    private readonly List<MealItem> _items = [];

    private Meal()
    {
    }

    public Meal(int dayNo, string name, TimeOnly time)
    {
        if (dayNo is < 1 or > 7)
        {
            throw new DomainException("Öğünün günü 1 ile 7 arasında olmalı.");
        }

        Id = Guid.CreateVersion7();
        DayNo = dayNo;
        Name = Guard.Required(name, 50, "Öğün adı");
        Time = time;
    }

    public Guid Id { get; private set; }

    /// <summary>Haftalık planda 1–7, günlük planda hep 1.</summary>
    public int DayNo { get; private set; }

    /// <summary>Kahvaltı, Ara öğün, Öğle ...</summary>
    public string Name { get; private set; } = null!;

    /// <summary>Öğün saati (veritabanında meal_time).</summary>
    public TimeOnly Time { get; private set; }

    public IReadOnlyCollection<MealItem> Items => _items.AsReadOnly();

    /// <summary>
    /// Satır ekler. Alternatif satır yalnızca bu öğündeki bir ana satıra bağlanabilir (BR-06).
    /// </summary>
    public void AddItem(MealItem item)
    {
        if (item.IsAlternative)
        {
            var target = _items.FirstOrDefault(i => i.Id == item.AlternativeToItemId);
            if (target is null || target.IsAlternative)
            {
                throw new DomainException("Alternatif, aynı öğündeki bir ana satıra bağlanmalı.");
            }
        }

        _items.Add(item);
    }

    /// <summary>Öğün toplamı = ana satırların toplamı (alternatifler sayılmaz).</summary>
    public NutrientTotals CalculateTotals() =>
        _items.Where(i => !i.IsAlternative)
            .Aggregate(NutrientTotals.Zero, (sum, item) => sum.Add(item.CalculateTotals()));

    /// <summary>
    /// Derin kopya: öğün ve bütün satırları yeni kimlik alır, alternatif bağlantıları
    /// kopyadaki yeni satırlara taşınır. Kopyada yapılan değişiklik aslını etkilemez (BR-07).
    /// </summary>
    public Meal Clone()
    {
        var copy = new Meal(DayNo, Name, Time);
        var newIds = new Dictionary<Guid, Guid>();

        foreach (var item in _items)
        {
            var itemCopy = item.Clone();
            newIds[item.Id] = itemCopy.Id;
            copy._items.Add(itemCopy);
        }

        foreach (var itemCopy in copy._items.Where(i => i.IsAlternative))
        {
            itemCopy.PointAlternativeTo(newIds[itemCopy.AlternativeToItemId!.Value]);
        }

        return copy;
    }
}
