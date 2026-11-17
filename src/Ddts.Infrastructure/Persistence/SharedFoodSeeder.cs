using Ddts.Domain.Foods;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Ddts.Infrastructure.Persistence;

/// <summary>
/// Ortak (hazır yüklenen) besinler (FR-D2): herkes kullanır, kimse düzenleyemez (dietitian_id boş).
/// Veritabanında hiç ortak besin yoksa bir kez eklenir. Değerler 100 g içindir; USDA FoodData Central'daki
/// (SR Legacy) genel karşılıklarından yuvarlanarak alındı. Kişisel veri içermez.
/// </summary>
public static class SharedFoodSeeder
{
    private static readonly (string Name, string Portion, decimal Grams, decimal Kcal, decimal Protein, decimal Carbs, decimal Fat)[] Foods =
    [
        ("Yulaf ezmesi", "1 yemek kaşığı", 10m, 379m, 13.2m, 67.7m, 6.5m),
        ("Tam buğday ekmeği", "1 dilim", 25m, 252m, 12.5m, 42.7m, 3.5m),
        ("Beyaz ekmek", "1 dilim", 25m, 266m, 8.9m, 49.4m, 3.3m),
        ("Pirinç pilavı", "1 porsiyon", 150m, 130m, 2.7m, 28.2m, 0.3m),
        ("Bulgur pilavı", "1 porsiyon", 150m, 83m, 3.1m, 18.6m, 0.2m),
        ("Haşlanmış makarna", "1 porsiyon", 150m, 158m, 5.8m, 30.9m, 0.9m),
        ("Haşlanmış patates", "1 orta boy", 150m, 87m, 1.9m, 20.1m, 0.1m),
        ("Yumurta", "1 adet", 50m, 143m, 12.6m, 0.7m, 9.5m),
        ("Beyaz peynir", "1 dilim", 30m, 264m, 14.2m, 4.1m, 21.3m),
        ("Kaşar peyniri", "1 dilim", 20m, 403m, 22.9m, 3.1m, 33.3m),
        ("Süt (yarım yağlı)", "1 su bardağı", 200m, 50m, 3.3m, 4.8m, 2.0m),
        ("Yoğurt (tam yağlı)", "1 kase", 150m, 61m, 3.5m, 4.7m, 3.3m),
        ("Zeytinyağı", "1 tatlı kaşığı", 5m, 884m, 0m, 0m, 100m),
        ("Siyah zeytin", "5 adet", 15m, 115m, 0.8m, 6.3m, 10.7m),
        ("Tavuk göğsü (ızgara)", "1 porsiyon", 120m, 165m, 31.0m, 0m, 3.6m),
        ("Dana kıyma (pişmiş)", "1 porsiyon", 100m, 250m, 25.9m, 0m, 15.4m),
        ("Somon (fırında)", "1 porsiyon", 120m, 206m, 22.1m, 0m, 12.4m),
        ("Kırmızı mercimek (pişmiş)", "1 kase", 200m, 116m, 9.0m, 20.1m, 0.4m),
        ("Nohut (haşlanmış)", "1 kase", 160m, 164m, 8.9m, 27.4m, 2.6m),
        ("Kuru fasulye (haşlanmış)", "1 kase", 180m, 127m, 8.7m, 22.8m, 0.5m),
        ("Domates", "1 orta boy", 120m, 18m, 0.9m, 3.9m, 0.2m),
        ("Salatalık", "1 orta boy", 150m, 15m, 0.7m, 3.6m, 0.1m),
        ("Ispanak (pişmiş)", "1 porsiyon", 180m, 23m, 3.0m, 3.8m, 0.3m),
        ("Brokoli (haşlanmış)", "1 porsiyon", 150m, 35m, 2.4m, 7.2m, 0.4m),
        ("Elma", "1 orta boy", 180m, 52m, 0.3m, 13.8m, 0.2m),
        ("Muz", "1 orta boy", 120m, 89m, 1.1m, 22.8m, 0.3m),
        ("Portakal", "1 orta boy", 150m, 47m, 0.9m, 11.8m, 0.1m),
        ("Ceviz içi", "3 adet", 12m, 654m, 15.2m, 13.7m, 65.2m),
        ("Badem", "10 adet", 12m, 579m, 21.2m, 21.6m, 49.9m),
        ("Bal", "1 tatlı kaşığı", 7m, 304m, 0.3m, 82.4m, 0m),
    ];

    public static async Task SeedSharedFoodsAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<DdtsDbContext>();

        if (await db.FoodItems.AnyAsync(f => f.DietitianId == null, cancellationToken))
        {
            return;
        }

        foreach (var food in Foods)
        {
            db.FoodItems.Add(new FoodItem(
                dietitianId: null,
                food.Name,
                food.Portion,
                food.Grams,
                food.Kcal,
                food.Protein,
                food.Carbs,
                food.Fat,
                sourceName: "Yerel",
                externalId: null,
                sourceUpdatedAt: null));
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
