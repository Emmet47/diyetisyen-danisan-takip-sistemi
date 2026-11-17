using Ddts.Domain.Clients;
using Ddts.Domain.Common;
using Ddts.Domain.Notifications;
using Ddts.Domain.Plans;

namespace Ddts.UnitTests.Domain;

/// <summary>Besin, şablon ve bildirim izni güncellemeleri (FR-D2, FR-F1, FR-J2).</summary>
public sealed class GuncellemeTests
{
    [Fact]
    public void Besin_duzeltilir_hatali_degerde_hic_degismez()
    {
        var besin = TestVerisi.Yumurta();

        besin.Update("Yumurta (haşlanmış)", "1 büyük", 60m, 150m, 12.5m, 1.1m, 10m);
        Assert.Equal("Yumurta (haşlanmış)", besin.Name);
        Assert.Equal(60m, besin.PortionGrams);

        Assert.Throws<DomainException>(() => besin.Update("Hatalı", "1 adet", 50m, 150m, 60m, 30m, 20m));   // makro toplamı 110 g
        Assert.Equal("Yumurta (haşlanmış)", besin.Name);
        Assert.Equal("Yerel", besin.SourceName);
    }

    [Fact]
    public void Sablon_guncellenince_daha_once_kopyalanan_plan_degismez()
    {
        var diyetisyen = TestVerisi.Diyetisyen();
        var danisan = TestVerisi.Danisan(diyetisyen);
        var sablon = new PlanTemplate(diyetisyen.Id, "Akdeniz", null, PlanType.Daily);
        var ogun = new Meal(1, "Kahvaltı", new TimeOnly(8, 0));
        ogun.AddItem(new MealItem(TestVerisi.Yumurta(), 2m));
        sablon.AddMeal(ogun);

        var kopya = sablon.CreatePlanFor(danisan, "Kopya");

        var yeniOgun = new Meal(1, "Ara öğün", new TimeOnly(10, 30));
        yeniOgun.AddItem(new MealItem(TestVerisi.Peynir(), 1m));
        sablon.Update("Akdeniz 2", "Hafif", PlanType.Daily, [yeniOgun]);

        Assert.Equal("Akdeniz 2", sablon.Name);
        Assert.Equal("Ara öğün", Assert.Single(sablon.Meals).Name);
        Assert.Equal("Kahvaltı", Assert.Single(kopya.Meals).Name);
        Assert.Throws<DomainException>(() => sablon.Update("Akdeniz", null, PlanType.Daily, [new Meal(2, "Akşam", new TimeOnly(19, 0))]));
    }

    [Fact]
    public void Danisanin_kapattigi_bildirim_turune_izin_verilmez()
    {
        var danisan = TestVerisi.Danisan(TestVerisi.Diyetisyen());

        danisan.ChangeNotificationSettings(new NotificationSettings(Appointments: true, MealReminders: false, Measurements: true, Plans: false));

        Assert.True(danisan.AllowsNotification(NotificationType.AppointmentReminder));
        Assert.False(danisan.AllowsNotification(NotificationType.MealLogReminder));
        Assert.False(danisan.AllowsNotification(NotificationType.PlanPublished));
        Assert.True(danisan.AllowsNotification(NotificationType.MeasurementReminder));
    }
}
