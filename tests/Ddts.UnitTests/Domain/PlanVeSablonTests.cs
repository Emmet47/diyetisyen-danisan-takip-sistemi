using Ddts.Domain.Common;
using Ddts.Domain.Plans;

namespace Ddts.UnitTests.Domain;

/// <summary>
/// Plan durumları (UT-12, BR-04) ve şablondan kopya (Prototype, UT-07, BR-07).
/// </summary>
public sealed class PlanVeSablonTests
{
    [Fact]
    public void Yayimlanan_plan_PlanPublished_olayi_uretir()
    {
        var plan = OgunluPlan();

        plan.Publish(TestVerisi.Simdi);

        Assert.Equal(PlanStatus.Published, plan.Status);
        Assert.Equal(TestVerisi.Simdi, plan.PublishedAt);
        var olay = Assert.IsType<PlanPublished>(Assert.Single(plan.DomainEvents));
        Assert.Equal(plan.Id, olay.PlanId);
        Assert.Equal(plan.ClientId, olay.ClientId);
        Assert.Equal(plan.DietitianId, olay.DietitianId);
    }

    [Fact]
    public void Ogunu_olmayan_plan_yayimlanamaz()
    {
        var diyetisyen = TestVerisi.Diyetisyen();
        var plan = NutritionPlan.CreateDraft(TestVerisi.Danisan(diyetisyen), "Boş plan", PlanType.Daily, null);

        Assert.Throws<DomainException>(() => plan.Publish(TestVerisi.Simdi));
    }

    [Fact]
    public void Yayimlanmis_plan_degistirilemez()
    {
        var plan = OgunluPlan();
        plan.Publish(TestVerisi.Simdi);

        Assert.Throws<DomainException>(() => plan.AddMeal(new Meal(1, "Akşam", new TimeOnly(19, 0))));
        Assert.Throws<DomainException>(() => plan.Publish(TestVerisi.Simdi));
    }

    [Fact]
    public void Yalnizca_yayimlanmis_plan_arsivlenir_ve_arsiv_geri_acilmaz()
    {
        var plan = OgunluPlan();
        Assert.Throws<DomainException>(() => plan.Archive(TestVerisi.Simdi));

        plan.Publish(TestVerisi.Simdi);
        plan.Archive(TestVerisi.Simdi.AddDays(7));

        Assert.Equal(PlanStatus.Archived, plan.Status);
        Assert.Throws<DomainException>(() => plan.Publish(TestVerisi.Simdi.AddDays(8)));
    }

    [Fact]
    public void Gunluk_plana_ikinci_gun_ogunu_eklenemez()
    {
        var diyetisyen = TestVerisi.Diyetisyen();
        var plan = NutritionPlan.CreateDraft(TestVerisi.Danisan(diyetisyen), "Günlük", PlanType.Daily, null);

        Assert.Throws<DomainException>(() => plan.AddMeal(new Meal(2, "Kahvaltı", new TimeOnly(8, 0))));
    }

    [Fact]
    public void Sablondan_olusan_plan_bagimsiz_kopyadir()
    {
        var diyetisyen = TestVerisi.Diyetisyen();
        var danisan = TestVerisi.Danisan(diyetisyen);
        var sablon = new PlanTemplate(diyetisyen.Id, "Kahvaltı ağırlıklı", null, PlanType.Daily);
        var sablonOgunu = new Meal(1, "Kahvaltı", new TimeOnly(8, 0));
        var anaSatir = new MealItem(TestVerisi.Yumurta(), 2m);
        sablonOgunu.AddItem(anaSatir);
        sablonOgunu.AddItem(new MealItem(TestVerisi.Peynir(), 2m, anaSatir.Id));
        sablon.AddMeal(sablonOgunu);

        var plan = sablon.CreatePlanFor(danisan, "Danışana özel plan");

        Assert.Equal(PlanStatus.Draft, plan.Status);
        Assert.Equal(sablon.Id, plan.SourceTemplateId);
        Assert.Equal(danisan.Id, plan.ClientId);

        var planOgunu = Assert.Single(plan.Meals);
        Assert.NotEqual(sablonOgunu.Id, planOgunu.Id);
        Assert.DoesNotContain(planOgunu.Items, i => sablonOgunu.Items.Any(s => s.Id == i.Id));

        // Kopyadaki alternatif, kopyadaki yeni ana satırı gösterir
        var kopyaAna = Assert.Single(planOgunu.Items, i => !i.IsAlternative);
        var kopyaAlternatif = Assert.Single(planOgunu.Items, i => i.IsAlternative);
        Assert.Equal(kopyaAna.Id, kopyaAlternatif.AlternativeToItemId);

        // Kopyaya eklenen satır şablonu etkilemez
        planOgunu.AddItem(new MealItem(TestVerisi.Peynir(), 1m));
        Assert.Equal(2, sablonOgunu.Items.Count);
        Assert.Equal(155m, sablon.CalculateTotals().EnergyKcal);
    }

    [Fact]
    public void Sablon_baska_diyetisyenin_danisanina_kopyalanamaz()
    {
        var sablon = new PlanTemplate(TestVerisi.Diyetisyen().Id, "Şablon", null, PlanType.Daily);
        var baskaDanisan = TestVerisi.Danisan(TestVerisi.Diyetisyen());

        Assert.Throws<DomainException>(() => sablon.CreatePlanFor(baskaDanisan, "Kopya"));
    }

    private static NutritionPlan OgunluPlan()
    {
        var diyetisyen = TestVerisi.Diyetisyen();
        var plan = NutritionPlan.CreateDraft(TestVerisi.Danisan(diyetisyen), "Günlük plan", PlanType.Daily, null);
        var ogun = new Meal(1, "Kahvaltı", new TimeOnly(8, 0));
        ogun.AddItem(new MealItem(TestVerisi.Yumurta(), 2m));
        plan.AddMeal(ogun);
        return plan;
    }
}
