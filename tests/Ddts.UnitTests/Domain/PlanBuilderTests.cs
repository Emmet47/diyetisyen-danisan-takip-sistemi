using Ddts.Domain.Clients;
using Ddts.Domain.Common;
using Ddts.Domain.Plans;

namespace Ddts.UnitTests.Domain;

/// <summary>Builder deseni (Şekil 4.6, UT-05, BR-05).</summary>
public sealed class PlanBuilderTests
{
    [Fact]
    public void Gecerli_adimlarla_taslak_plan_kurulur_sevmedigi_besin_uyari_verir()
    {
        var diyetisyen = TestVerisi.Diyetisyen();
        var danisan = TestVerisi.Danisan(diyetisyen);
        var yumurta = TestVerisi.Yumurta();
        var peynir = TestVerisi.Peynir();
        danisan.SetRestrictions([new FoodRestriction(peynir.Id, RestrictionType.Dislike)]);

        var builder = new NutritionPlanBuilder()
            .ForClient(danisan)
            .WithTitle("Kasım planı")
            .WithType(PlanType.Weekly)
            .WithEnergyTarget(1800, "MifflinStJeor")
            .AddMeal(1, "Kahvaltı", new TimeOnly(8, 0))
            .AddFood(yumurta, 2m)
            .AddAlternative(peynir, 2m)
            .AddMeal(2, "Kahvaltı", new TimeOnly(8, 0))
            .AddFood(yumurta, 1m);

        var plan = builder.Build();

        Assert.Equal(PlanStatus.Draft, plan.Status);
        Assert.Equal(danisan.Id, plan.ClientId);
        Assert.Equal(1800, plan.TargetKcal);
        Assert.Equal(2, plan.Meals.Count);
        Assert.Equal(2, plan.Meals.First().Items.Count);
        Assert.Equal("Beyaz peynir: danışan bu besini sevmiyor.", Assert.Single(builder.Warnings()));

        // Alternatif toplama katılmaz: 2 yumurta (100 g, 155 kcal) + 1 yumurta (50 g, 77,5 kcal)
        Assert.Equal(232.5m, plan.CalculateTotals().EnergyKcal);
    }

    [Fact]
    public void Alerjen_besin_ve_eksikler_tek_hata_listesinde_birlikte_bildirilir()
    {
        var diyetisyen = TestVerisi.Diyetisyen();
        var danisan = TestVerisi.Danisan(diyetisyen);
        var yumurta = TestVerisi.Yumurta();
        danisan.SetRestrictions([new FoodRestriction(yumurta.Id, RestrictionType.Allergy)]);

        var builder = new NutritionPlanBuilder()
            .ForClient(danisan)
            .WithTitle(" ")
            .WithType(PlanType.Daily)
            .AddMeal(1, "Kahvaltı", new TimeOnly(8, 0))
            .AddFood(yumurta, 2m)
            .AddMeal(2, "Öğle", new TimeOnly(12, 30))
            .AddFood(TestVerisi.Peynir(), 1m);

        var hata = Assert.Throws<DomainException>(() => builder.Build());

        Assert.Contains(hata.Errors, e => e.Contains("alerji listesinde"));
        Assert.Contains(hata.Errors, e => e == "Plan başlığı boş olamaz.");
        Assert.Contains(hata.Errors, e => e.Contains("Kahvaltı: öğünde en az bir besin olmalı"));
        Assert.Contains(hata.Errors, e => e.Contains("günlük planda öğünler 1. güne eklenir"));
    }

    [Fact]
    public void Ogunsuz_ve_danisansiz_plan_kurulamaz()
    {
        var hata = Assert.Throws<DomainException>(() => new NutritionPlanBuilder().WithTitle("Plan").Build());

        Assert.Contains(hata.Errors, e => e == "Planın danışanı seçilmeli.");
        Assert.Contains(hata.Errors, e => e == "Plana en az bir öğün eklenmeli.");
    }

    [Fact]
    public void Taslak_plan_yeni_icerikle_guncellenir_yayimlanmis_plan_guncellenemez()
    {
        var diyetisyen = TestVerisi.Diyetisyen();
        var danisan = TestVerisi.Danisan(diyetisyen);
        var plan = YeniPlan(danisan, "İlk", 2m);
        var planId = plan.Id;

        plan.UpdateDraft(YeniPlan(danisan, "Düzeltilmiş", 1m));

        Assert.Equal(planId, plan.Id);
        Assert.Equal("Düzeltilmiş", plan.Title);
        Assert.Equal(1m, Assert.Single(Assert.Single(plan.Meals).Items).PortionCount);

        plan.Publish(TestVerisi.Simdi);
        Assert.Throws<DomainException>(() => plan.UpdateDraft(YeniPlan(danisan, "Tekrar", 3m)));
    }

    private static NutritionPlan YeniPlan(Client danisan, string baslik, decimal porsiyon) => new NutritionPlanBuilder()
        .ForClient(danisan)
        .WithTitle(baslik)
        .WithType(PlanType.Daily)
        .AddMeal(1, "Kahvaltı", new TimeOnly(8, 0))
        .AddFood(TestVerisi.Yumurta(), porsiyon)
        .Build();
}
