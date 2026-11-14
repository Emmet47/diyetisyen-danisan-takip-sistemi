using Ddts.Domain.Common;
using Ddts.Domain.Subscriptions;

namespace Ddts.UnitTests.Domain;

/// <summary>Paket limiti ve abonelik durumları (UT-06, BR-01).</summary>
public sealed class AbonelikTests
{
    private static readonly DateOnly Bugun = new(2026, 12, 1);

    [Fact]
    public void Limit_dolmadan_danisan_eklenebilir_dolunca_eklenemez()
    {
        var abonelik = BaslangicAboneligi();   // en fazla 10 danışan

        abonelik.EnsureCanAddClient(9);

        var hata = Assert.Throws<SubscriptionLimitException>(() => abonelik.EnsureCanAddClient(10));
        Assert.StartsWith("Başlangıç paketinde en fazla 10 aktif danışan olabilir.", hata.Message);
    }

    [Fact]
    public void Limitin_yuzde_80ine_gelince_uyari_verilir()
    {
        var abonelik = BaslangicAboneligi();

        Assert.False(abonelik.IsNearLimit(7));
        Assert.True(abonelik.IsNearLimit(8));
    }

    [Fact]
    public void Aktif_olmayan_abonelikte_danisan_eklenemez()
    {
        var abonelik = BaslangicAboneligi();

        abonelik.Expire();

        Assert.Throws<SubscriptionLimitException>(() => abonelik.EnsureCanAddClient(0));
        Assert.False(abonelik.IsActiveOn(Bugun));
    }

    [Fact]
    public void Abonelik_bir_aylik_baslar_yenilenince_bir_ay_uzar()
    {
        var abonelik = BaslangicAboneligi();
        Assert.Equal(new DateOnly(2027, 1, 1), abonelik.EndDate);
        Assert.True(abonelik.IsActiveOn(new DateOnly(2027, 1, 1)));
        Assert.False(abonelik.IsActiveOn(new DateOnly(2027, 1, 2)));

        abonelik.Renew();

        Assert.Equal(new DateOnly(2027, 2, 1), abonelik.EndDate);
    }

    [Fact]
    public void Iptal_edilen_abonelik_yenilenemez()
    {
        var abonelik = BaslangicAboneligi();

        abonelik.Cancel();

        Assert.Equal(SubscriptionStatus.Cancelled, abonelik.Status);
        Assert.Throws<DomainException>(abonelik.Renew);
        Assert.Throws<DomainException>(abonelik.Cancel);
    }

    [Fact]
    public void Yonetici_paketi_duzenler_ve_satistan_kaldirir()
    {
        var paket = new SubscriptionPlan("Başlangıç", 10, 299m);

        paket.Update("Başlangıç Plus", 15, 349.90m, isActive: false);

        Assert.Equal("Başlangıç Plus", paket.Name);
        Assert.Equal(15, paket.MaxClients);
        Assert.Equal(349.90m, paket.MonthlyPrice);
        Assert.False(paket.IsActive);
        Assert.Throws<DomainException>(() => paket.Update("Boş", 0, 100m, true));
        Assert.Throws<DomainException>(() => paket.Update(" ", 10, 100m, true));
        Assert.Throws<DomainException>(() => paket.Update("Pahalı", 10, -1m, true));
    }

    [Fact]
    public void Limit_dusurulen_pakette_yeni_limit_gecerli_olur()
    {
        var abonelik = BaslangicAboneligi();   // en fazla 10 danışan

        abonelik.Plan.Update("Başlangıç", 5, 299m, isActive: true);

        Assert.Throws<SubscriptionLimitException>(() => abonelik.EnsureCanAddClient(5));
        Assert.True(abonelik.IsNearLimit(4));
    }

    private static Subscription BaslangicAboneligi() =>
        new(TestVerisi.Diyetisyen().Id, new SubscriptionPlan("Başlangıç", 10, 299m), Bugun);
}
