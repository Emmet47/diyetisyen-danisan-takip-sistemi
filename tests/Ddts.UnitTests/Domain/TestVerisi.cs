using Ddts.Domain.Clients;
using Ddts.Domain.Dietitians;
using Ddts.Domain.Foods;

namespace Ddts.UnitTests.Domain;

/// <summary>Birim testlerinde ortak kullanılan sahte veriler (gerçek kişi ya da sağlık bilgisi yok).</summary>
internal static class TestVerisi
{
    /// <summary>1 Aralık 2026 10.00 (Türkiye) = 07.00 UTC</summary>
    public static readonly DateTime Simdi = new(2026, 12, 1, 7, 0, 0, DateTimeKind.Utc);

    public static Dietitian Diyetisyen() => new(Guid.CreateVersion7(), "Dyt. Ayşe Örnek", phone: null);

    public static Client Danisan(Dietitian diyetisyen) => new(
        diyetisyen.Id,
        Guid.CreateVersion7(),
        new ClientProfile("Deneme Danışan", Phone: null, GoalType.LoseWeight, TargetWeightKg: 65m, Preferences: null, AllergyNotes: null),
        new DateOnly(1995, 1, 1),
        Sex.Female,
        Simdi);

    /// <summary>1 adet = 50 g, 100 g'da 155 kcal</summary>
    public static FoodItem Yumurta() =>
        new(null, "Haşlanmış yumurta", "1 adet", 50m, 155m, 12.6m, 1.1m, 10.6m, "Yerel", null, null);

    /// <summary>1 dilim = 30 g, 100 g'da 264 kcal</summary>
    public static FoodItem Peynir() =>
        new(null, "Beyaz peynir", "1 dilim", 30m, 264m, 17m, 2m, 21m, "Yerel", null, null);
}
