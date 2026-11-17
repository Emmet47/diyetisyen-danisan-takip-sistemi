using Ddts.Domain.Clients;
using Ddts.Domain.Common;
using Ddts.Domain.Energy;

namespace Ddts.UnitTests.Domain;

/// <summary>Enerji stratejileri (Strategy, Şekil 4.7): UT-01 Mifflin-St Jeor, UT-02 Harris-Benedict, UT-03 bağlam ve sınırlar.</summary>
public sealed class EnerjiTests
{
    private static readonly EnergyCalculator Hesaplayici = new([new MifflinStJeorStrategy(), new HarrisBenedictStrategy()]);

    [Fact]
    public void Mifflin_St_Jeor_kadin_ve_erkek_icin_dogru_BMH_verir()
    {
        var strateji = new MifflinStJeorStrategy();

        // Kadın: 10·70 + 6,25·165 − 5·30 − 161 = 1420,25 (docs/api.md örnek 3)
        Assert.Equal(1420.25m, strateji.CalculateBmr(new EnergyInput(Sex.Female, 30, 70m, 165m)));
        // Erkek: 10·80 + 6,25·180 − 5·40 + 5 = 1730
        Assert.Equal(1730m, strateji.CalculateBmr(new EnergyInput(Sex.Male, 40, 80m, 180m)));
    }

    [Fact]
    public void Harris_Benedict_kadin_ve_erkek_icin_dogru_BMH_verir()
    {
        var strateji = new HarrisBenedictStrategy();

        // Kadın: 447,593 + 9,247·70 + 3,098·165 − 4,330·30 = 1476,153
        Assert.Equal(1476.153m, strateji.CalculateBmr(new EnergyInput(Sex.Female, 30, 70m, 165m)));
        // Erkek: 88,362 + 13,397·80 + 4,799·180 − 5,677·40 = 1796,862
        Assert.Equal(1796.862m, strateji.CalculateBmr(new EnergyInput(Sex.Male, 40, 80m, 180m)));
    }

    [Fact]
    public void Baglam_secilen_formulu_kullanir_gunluk_ihtiyac_ve_hedefi_hesaplar()
    {
        var sonuc = Hesaplayici.Calculate("MifflinStJeor", new EnergyInput(Sex.Female, 30, 70m, 165m), 1.55m, -500);

        Assert.Equal("MifflinStJeor", sonuc.Formula);
        Assert.Equal(1420.25m, sonuc.BmrKcal);
        Assert.Equal(2201.39m, sonuc.DailyNeedKcal);   // 1420,25 × 1,55 = 2201,3875
        Assert.Equal(1701.39m, sonuc.TargetKcal);
        Assert.Equal("Hesaplamalar tahminidir, profesyonel değerlendirme yerine geçmez.", sonuc.Disclaimer);

        var diger = Hesaplayici.Calculate("harrisbenedict", new EnergyInput(Sex.Female, 30, 70m, 165m), 1.2m, 0);
        Assert.Equal("HarrisBenedict", diger.Formula);
        Assert.Equal(1476.15m, diger.BmrKcal);
    }

    [Fact]
    public void Bilinmeyen_formul_ve_sinir_disi_katsayi_reddedilir()
    {
        var girdi = new EnergyInput(Sex.Male, 40, 80m, 180m);

        Assert.Throws<DomainException>(() => Hesaplayici.Calculate("Katch", girdi, 1.5m, 0));
        Assert.Throws<DomainException>(() => Hesaplayici.Calculate("MifflinStJeor", girdi, 1.1m, 0));
        Assert.Throws<DomainException>(() => Hesaplayici.Calculate("MifflinStJeor", girdi, 2.0m, 0));
        Assert.Throws<DomainException>(() => Hesaplayici.Calculate("MifflinStJeor", girdi, 1.5m, -2000));
    }
}
