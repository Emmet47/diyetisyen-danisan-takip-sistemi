using Ddts.Application.Abstractions.Nutrition;
using Ddts.Application.Abstractions.Persistence;
using Ddts.Application.Abstractions.Security;
using Ddts.Application.Abstractions.Translation;
using Ddts.Application.Foods;
using Ddts.Domain.Common;
using Ddts.Domain.Foods;
using Ddts.Domain.Users;

namespace Ddts.UnitTests.Application;

/// <summary>
/// Besin arama ve aktarma (FR-D1, FR-D3, FR-D4, ADR-0007): birden çok dış kaynak, USDA için çeviri,
/// bir kaynak çalışmazken diğerlerinin sonuçları ve porsiyon başına gelen değerlerin 100 g'a çevrilmesi.
/// Kaynaklar ve çevirmen sahte (veritabanı ve ağ yok).
/// </summary>
public sealed class BesinAramaTests
{
    private static readonly Guid DiyetisyenId = Guid.NewGuid();
    private static readonly DateTime Alindi = new(2026, 11, 20, 10, 0, 0, DateTimeKind.Utc);

    private static CancellationToken Iptal => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Usda_sorgusu_ingilizceye_gelen_adlar_turkceye_cevrilir()
    {
        var usda = new SahteKaynak("USDA FoodData Central", "en", Usda("173904", "Lentils, cooked"));
        var cevirmen = new SahteCevirmen(("mercimek", "lentils"), ("Lentils, cooked", "Mercimek, pişmiş"));
        var servis = Servis(cevirmen, usda);

        var sonuc = await servis.SearchAsync("mercimek", source: null, includeExternal: true, Iptal);

        Assert.Equal("lentils", usda.SonSorgu);
        var bulunan = Assert.Single(sonuc.Items);
        Assert.Equal("Mercimek, pişmiş", bulunan.Name);
        Assert.False(sonuc.ExternalUnavailable);
    }

    [Fact]
    public async Task Turkce_kaynaga_sorgu_cevrilmeden_gider()
    {
        var plateRank = new SahteKaynak("PlateRank", "tr", PorsiyonBasina("pr-1", "Mercimek çorbası"));
        var cevirmen = new SahteCevirmen();
        var servis = Servis(cevirmen, plateRank);

        var sonuc = await servis.SearchAsync("mercimek", source: null, includeExternal: true, Iptal);

        Assert.Equal("mercimek", plateRank.SonSorgu);
        Assert.Equal(0, cevirmen.CagriSayisi);
        Assert.Equal("Mercimek çorbası", Assert.Single(sonuc.Items).Name);
    }

    [Fact]
    public async Task Bir_dis_kaynak_calismazsa_digerinin_sonuclari_uyariyla_doner()
    {
        var plateRank = new SahteKaynak("PlateRank", "tr", PorsiyonBasina("pr-1", "Ezogelin çorbası"));
        var usda = new SahteKaynak("USDA FoodData Central", "en") { Calismiyor = true };
        var servis = Servis(new SahteCevirmen(), plateRank, usda);

        var sonuc = await servis.SearchAsync("ezogelin", source: null, includeExternal: true, Iptal);

        Assert.True(sonuc.ExternalUnavailable);
        Assert.Equal("USDA FoodData Central kaynağına şu an ulaşılamıyor, diğer sonuçlar gösteriliyor.", sonuc.Message);
        Assert.Equal("Ezogelin çorbası", Assert.Single(sonuc.Items).Name);
    }

    [Fact]
    public async Task Hicbir_dis_kaynak_calismazsa_yerel_sonuclar_genel_mesajla_doner()
    {
        var plateRank = new SahteKaynak("PlateRank", "tr") { Calismiyor = true };
        var usda = new SahteKaynak("USDA FoodData Central", "en") { Calismiyor = true };
        var yerel = new SahteKaynak("Yerel", "tr", YerelBesin("Yulaf ezmesi"));
        var servis = Servis(new SahteCevirmen(), yerel, plateRank, usda);

        var sonuc = await servis.SearchAsync("yulaf", source: null, includeExternal: true, Iptal);

        Assert.True(sonuc.ExternalUnavailable);
        Assert.Equal(FoodSearchService.ExternalUnavailableMessage, sonuc.Message);
        Assert.Equal("Yulaf ezmesi", Assert.Single(sonuc.Items).Name);
    }

    [Fact]
    public async Task Kaynak_suzgeci_yalnizca_secilen_dis_kaynagi_sorar()
    {
        var plateRank = new SahteKaynak("PlateRank", "tr", PorsiyonBasina("pr-1", "Kısır"));
        var usda = new SahteKaynak("USDA FoodData Central", "en", Usda("1", "Bulgur"));
        var servis = Servis(new SahteCevirmen(), plateRank, usda);

        var sonuc = await servis.SearchAsync("kısır", source: "PlateRank", includeExternal: true, Iptal);

        Assert.Null(usda.SonSorgu);
        Assert.Equal("PlateRank", Assert.Single(sonuc.Items).SourceName);

        var yalnizYerel = await servis.SearchAsync("kısır", source: "Yerel", includeExternal: true, Iptal);
        Assert.Empty(yalnizYerel.Items);
    }

    [Fact]
    public async Task Daha_once_aktarilan_dis_kayit_ikinci_kez_gosterilmez()
    {
        var aktarilmis = new NutritionItem(
            Guid.NewGuid(), "Kısır", "1 kase", 200m, 150m, 4m, 25m, 4m, "PlateRank", "pr-1", Alindi);
        var yerel = new SahteKaynak("Yerel", "tr", aktarilmis);
        var plateRank = new SahteKaynak("PlateRank", "tr", PorsiyonBasina("pr-1", "Kısır"), PorsiyonBasina("pr-2", "Kısır (acılı)"));
        var servis = Servis(new SahteCevirmen(), yerel, plateRank);

        var sonuc = await servis.SearchAsync("kısır", source: null, includeExternal: true, Iptal);

        Assert.Equal(new string?[] { "pr-1", "pr-2" }, sonuc.Items.Select(i => i.ExternalId));
        Assert.NotNull(sonuc.Items[0].Id);   // yerel kayıt
        Assert.Null(sonuc.Items[1].Id);      // dış sonuç
    }

    [Fact]
    public async Task Porsiyon_basina_gelen_degerler_girilen_gramla_100_grama_cevrilir()
    {
        // 1 porsiyon (250 g) = 300 kcal, 10 g protein, 40 g karbonhidrat, 10 g yağ → 100 g'da 120 / 4 / 16 / 4
        var plateRank = new SahteKaynak("PlateRank", "tr", PorsiyonBasina("pr-1", "Mercimek çorbası"));
        var depo = new SahteBesinDeposu();
        var servis = Servis(new SahteCevirmen(), depo, plateRank);

        await servis.ImportAsync(new ImportFoodCommand("pr-1", null, "1 kase", 250m, "PlateRank"), Iptal);

        var besin = Assert.Single(depo.Eklenenler);
        Assert.Equal("Mercimek çorbası", besin.Name);
        Assert.Equal(250m, besin.PortionGrams);
        Assert.Equal(120m, besin.EnergyKcalPer100g);
        Assert.Equal(4m, besin.ProteinPer100g);
        Assert.Equal(16m, besin.CarbsPer100g);
        Assert.Equal(4m, besin.FatPer100g);
        Assert.Equal("PlateRank", besin.SourceName);
        Assert.Equal("pr-1", besin.ExternalId);
    }

    [Fact]
    public async Task Porsiyon_grami_olmayan_sonuc_gram_girilmeden_aktarilamaz()
    {
        var plateRank = new SahteKaynak("PlateRank", "tr", PorsiyonBasina("pr-1", "Mercimek çorbası"));
        var depo = new SahteBesinDeposu();
        var servis = Servis(new SahteCevirmen(), depo, plateRank);

        await Assert.ThrowsAsync<DomainException>(() =>
            servis.ImportAsync(new ImportFoodCommand("pr-1", null, null, null, "PlateRank"), Iptal));
        Assert.Empty(depo.Eklenenler);
    }

    [Fact]
    public async Task Kaynak_verilmezse_usda_kullanilir_ve_ad_turkceye_cevrilir()
    {
        var plateRank = new SahteKaynak("PlateRank", "tr");
        var usda = new SahteKaynak("USDA FoodData Central", "en", Usda("173904", "Oats, rolled"));
        var depo = new SahteBesinDeposu();
        var servis = Servis(new SahteCevirmen(("Oats, rolled", "Yulaf ezmesi")), depo, plateRank, usda);

        await servis.ImportAsync(new ImportFoodCommand("173904", null, null, null), Iptal);

        var besin = Assert.Single(depo.Eklenenler);
        Assert.Equal("Yulaf ezmesi", besin.Name);
        Assert.Equal(100m, besin.PortionGrams);
        Assert.Equal(380m, besin.EnergyKcalPer100g);   // USDA değerleri zaten 100 g için: değişmez
        Assert.Equal("USDA FoodData Central", besin.SourceName);
    }

    private static FoodSearchService Servis(SahteCevirmen cevirmen, params SahteKaynak[] kaynaklar) =>
        Servis(cevirmen, new SahteBesinDeposu(), kaynaklar);

    private static FoodSearchService Servis(SahteCevirmen cevirmen, SahteBesinDeposu depo, params SahteKaynak[] kaynaklar)
    {
        var yerel = kaynaklar.FirstOrDefault(k => k.SourceName == "Yerel") ?? new SahteKaynak("Yerel", "tr");
        var dis = kaynaklar.Where(k => k.SourceName != "Yerel").ToList<NutritionDataPort>();
        return new FoodSearchService(yerel, dis, cevirmen, depo, new SahteBirimIs(), new SahteKullanici());
    }

    private static NutritionItem Usda(string id, string ad) =>
        new(null, ad, "100 g", 100m, 380m, 13m, 68m, 6.5m, "USDA FoodData Central", id, Alindi);

    private static NutritionItem PorsiyonBasina(string id, string ad) =>
        new(null, ad, "1 porsiyon", 0m, 300m, 10m, 40m, 10m, "PlateRank", id, Alindi, ValuesPerPortion: true);

    private static NutritionItem YerelBesin(string ad) =>
        new(Guid.NewGuid(), ad, "1 kase", 40m, 380m, 13m, 68m, 6.5m, "Yerel", null, null);

    private sealed class SahteKaynak(string ad, string dil, params NutritionItem[] sonuclar) : NutritionDataPort
    {
        public bool Calismiyor { get; init; }

        public string? SonSorgu { get; private set; }

        public string SourceName => ad;

        public string LanguageCode => dil;

        public Task<IReadOnlyList<NutritionItem>> SearchAsync(string query, int limit, CancellationToken cancellationToken = default)
        {
            SonSorgu = query;
            return Calismiyor
                ? throw new ExternalSourceUnavailableException(ad + " kullanılamıyor.")
                : Task.FromResult<IReadOnlyList<NutritionItem>>(sonuclar);
        }

        public Task<NutritionItem?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken = default) =>
            Calismiyor
                ? throw new ExternalSourceUnavailableException(ad + " kullanılamıyor.")
                : Task.FromResult(sonuclar.FirstOrDefault(s => s.ExternalId == externalId));
    }

    /// <summary>Verilen çiftleri çevirir, bilmediği metni olduğu gibi döndürür (gerçek adapter'ın hata hali gibi).</summary>
    private sealed class SahteCevirmen(params (string Kaynak, string Hedef)[] ciftler) : ITranslator
    {
        public int CagriSayisi { get; private set; }

        public Task<IReadOnlyList<string>> TranslateAsync(
            IReadOnlyList<string> texts, string sourceLanguage, string targetLanguage, CancellationToken cancellationToken = default)
        {
            CagriSayisi++;
            IReadOnlyList<string> sonuc = texts
                .Select(t => ciftler.FirstOrDefault(c => c.Kaynak == t).Hedef ?? t)
                .ToList();
            return Task.FromResult(sonuc);
        }
    }

    private sealed class SahteBesinDeposu : IFoodItemRepository
    {
        public List<FoodItem> Eklenenler { get; } = [];

        public Task<IReadOnlyList<FoodItem>> GetManyAsync(Guid dietitianId, IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<FoodItem>>(Eklenenler.Where(f => ids.Contains(f.Id)).ToList());

        public Task<FoodItem?> GetAsync(Guid dietitianId, Guid foodItemId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Eklenenler.FirstOrDefault(f => f.Id == foodItemId));

        public Task<FoodItem?> GetByExternalIdAsync(Guid dietitianId, string sourceName, string externalId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Eklenenler.FirstOrDefault(f => f.SourceName == sourceName && f.ExternalId == externalId));

        public Task<bool> IsInUseAsync(Guid foodItemId, CancellationToken cancellationToken = default) => Task.FromResult(false);

        public void Add(FoodItem food) => Eklenenler.Add(food);

        public void Remove(FoodItem food) => Eklenenler.Remove(food);
    }

    private sealed class SahteBirimIs : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);

        public Task ExecuteInTransactionAsync(Func<Task> work, CancellationToken cancellationToken = default) => work();
    }

    private sealed class SahteKullanici : ICurrentUser
    {
        public Guid UserId { get; } = Guid.NewGuid();

        public UserRole Role => UserRole.Dietitian;

        public Guid? DietitianId => DiyetisyenId;

        public Guid? ClientId => null;
    }
}
