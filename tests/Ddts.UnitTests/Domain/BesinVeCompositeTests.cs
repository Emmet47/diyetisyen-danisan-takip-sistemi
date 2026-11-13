using Ddts.Domain.Common;
using Ddts.Domain.Foods;
using Ddts.Domain.Plans;

namespace Ddts.UnitTests.Domain;

/// <summary>
/// Besin değerleri ve Composite deseni (Şekil 4.5): plan → öğün → besin satırı toplamları.
/// UT-04: alternatif satırlar toplama katılmaz (BR-06).
/// </summary>
public sealed class BesinVeCompositeTests
{
    [Fact]
    public void NutrientsFor_100_gramdaki_degerleri_grama_gore_olcekler()
    {
        var yumurta = TestVerisi.Yumurta();

        var toplam = yumurta.NutrientsFor(50m);

        Assert.Equal(77.5m, toplam.EnergyKcal);
        Assert.Equal(6.3m, toplam.ProteinG);
        Assert.Equal(0.55m, toplam.CarbsG);
        Assert.Equal(5.3m, toplam.FatG);
    }

    [Fact]
    public void Makro_toplami_100_grami_gecen_besin_olusturulamaz()
    {
        Assert.Throws<DomainException>(() =>
            new FoodItem(null, "Hatalı", "1 adet", 10m, 500m, 60m, 30m, 20m, "Yerel", null, null));
    }

    [Fact]
    public void Dis_kaynak_kimligi_cekilme_zamani_olmadan_girilemez()
    {
        Assert.Throws<DomainException>(() =>
            new FoodItem(null, "Oats", "1 yemek kaşığı", 10m, 379m, 13.2m, 67.7m, 6.5m, "USDA FoodData Central", "173904", null));
    }

    [Fact]
    public void Makro_yuzdeleri_makro_enerjisine_gore_hesaplanir()
    {
        // 10 g protein = 40 kcal, 20 g karbonhidrat = 80 kcal, 10 g yağ = 90 kcal → toplam 210 kcal
        var toplam = new NutrientTotals(210m, 10m, 20m, 10m);

        Assert.Equal(19.0m, toplam.ProteinPercent);
        Assert.Equal(38.1m, toplam.CarbsPercent);
        Assert.Equal(42.9m, toplam.FatPercent);
        Assert.Equal(0m, NutrientTotals.Zero.ProteinPercent);
    }

    [Fact]
    public void Ogun_toplamina_alternatif_satir_katilmaz()
    {
        var ogun = new Meal(1, "Kahvaltı", new TimeOnly(8, 0));
        var anaSatir = new MealItem(TestVerisi.Yumurta(), 2m);            // 100 g → 155 kcal
        ogun.AddItem(anaSatir);
        ogun.AddItem(new MealItem(TestVerisi.Peynir(), 2m, anaSatir.Id)); // alternatif, sayılmaz

        var toplam = ogun.CalculateTotals();

        Assert.Equal(155m, toplam.EnergyKcal);
        Assert.Equal(12.6m, toplam.ProteinG);
    }

    [Fact]
    public void Alternatif_baska_ogundeki_satira_baglanamaz()
    {
        var kahvalti = new Meal(1, "Kahvaltı", new TimeOnly(8, 0));
        var anaSatir = new MealItem(TestVerisi.Yumurta(), 1m);
        kahvalti.AddItem(anaSatir);
        var ogle = new Meal(1, "Öğle", new TimeOnly(12, 30));

        Assert.Throws<DomainException>(() => ogle.AddItem(new MealItem(TestVerisi.Peynir(), 1m, anaSatir.Id)));
    }

    [Fact]
    public void Alternatifin_alternatifi_eklenemez()
    {
        var ogun = new Meal(1, "Kahvaltı", new TimeOnly(8, 0));
        var anaSatir = new MealItem(TestVerisi.Yumurta(), 1m);
        ogun.AddItem(anaSatir);
        var alternatif = new MealItem(TestVerisi.Peynir(), 1m, anaSatir.Id);
        ogun.AddItem(alternatif);

        Assert.Throws<DomainException>(() => ogun.AddItem(new MealItem(TestVerisi.Peynir(), 2m, alternatif.Id)));
    }

    [Fact]
    public void Plan_toplami_ogunlerin_toplami_gun_toplami_o_gunun_ogunleri()
    {
        var diyetisyen = TestVerisi.Diyetisyen();
        var plan = NutritionPlan.CreateDraft(TestVerisi.Danisan(diyetisyen), "Haftalık plan", PlanType.Weekly, null);

        var pazartesi = new Meal(1, "Kahvaltı", new TimeOnly(8, 0));
        pazartesi.AddItem(new MealItem(TestVerisi.Yumurta(), 1m));        // 50 g → 77,5 kcal
        var sali = new Meal(2, "Kahvaltı", new TimeOnly(8, 0));
        sali.AddItem(new MealItem(TestVerisi.Peynir(), 1m));              // 30 g → 79,2 kcal
        plan.AddMeal(pazartesi);
        plan.AddMeal(sali);

        Assert.Equal(156.7m, plan.CalculateTotals().EnergyKcal);
        Assert.Equal(77.5m, plan.CalculateDayTotals(1).EnergyKcal);
        Assert.Equal(79.2m, plan.CalculateDayTotals(2).EnergyKcal);
        Assert.Equal(0m, plan.CalculateDayTotals(3).EnergyKcal);
    }

    [Fact]
    public void Porsiyon_sayisi_0_ile_20_arasinda_olmali()
    {
        Assert.Throws<DomainException>(() => new MealItem(TestVerisi.Yumurta(), 0m));
        Assert.Throws<DomainException>(() => new MealItem(TestVerisi.Yumurta(), 21m));
    }
}
