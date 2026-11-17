using System.Net;
using System.Net.Http.Json;
using static Ddts.IntegrationTests.TestYardimcisi;

namespace Ddts.IntegrationTests;

/// <summary>Besin, plan, öğün ve randevu testlerinin ortak adımları. Bütün değerler sahte (NFR-D1).</summary>
internal static class ModulYardimcisi
{
    /// <summary>Danışan ekler, daveti kabul ettirir ve danışan olarak giriş yapar (1 giriş).</summary>
    public static async Task<DanisanOturumu> DanisanHesabiAcAsync(ApiFactory factory, HttpClient diyetisyen)
    {
        var eposta = YeniEposta("danisan");
        var id = await DanisanEkleAsync(diyetisyen, eposta);
        await DavetiKabulEtAsync(factory, eposta);
        return new DanisanOturumu(id, await GirisYapmisIstemciAsync(factory, eposta));
    }

    /// <summary>Diyetisyenin kendi yerel besini (100 g için değerler).</summary>
    public static async Task<Guid> BesinEkleAsync(
        HttpClient diyetisyen, string ad, decimal porsiyonGram, decimal kcal, decimal protein, decimal karbonhidrat, decimal yag)
    {
        var yanit = await diyetisyen.PostAsJsonAsync("/api/foods", new
        {
            name = ad,
            portionName = "1 porsiyon",
            portionGrams = porsiyonGram,
            energyKcalPer100g = kcal,
            proteinPer100g = protein,
            carbsPer100g = karbonhidrat,
            fatPer100g = yag,
        }, Iptal);
        Assert.Equal(HttpStatusCode.Created, yanit.StatusCode);
        return (await yanit.Content.ReadFromJsonAsync<KimlikYaniti>(Iptal))!.Id;
    }

    /// <summary>Tek öğünlü günlük plan gövdesi: kahvaltıda ana besin ve (varsa) alternatifi.</summary>
    public static object GunlukPlanFormu(Guid danisanId, string baslik, Guid besinId, decimal porsiyon, Guid? alternatifId = null) => new
    {
        clientId = danisanId,
        title = baslik,
        planType = "Daily",
        targetKcal = 1800,
        energyFormula = "MifflinStJeor",
        meals = new[]
        {
            new
            {
                dayNo = 1,
                name = "Kahvaltı",
                time = "08:00:00",
                items = new[]
                {
                    new
                    {
                        foodItemId = besinId,
                        portionCount = porsiyon,
                        alternatives = alternatifId is { } a ? new[] { new { foodItemId = a, portionCount = 1m } } : [],
                    },
                },
            },
        },
    };

    public static async Task<Guid> TaslakPlanKurAsync(HttpClient diyetisyen, Guid danisanId, Guid besinId)
    {
        var yanit = await diyetisyen.PostAsJsonAsync("/api/plans", GunlukPlanFormu(danisanId, "Deneme planı", besinId, 2m), Iptal);
        Assert.Equal(HttpStatusCode.Created, yanit.StatusCode);
        return (await yanit.Content.ReadFromJsonAsync<PlanSonucu>(Iptal))!.PlanId;
    }

    public static async Task<Guid> YayimlanmisPlanKurAsync(HttpClient diyetisyen, Guid danisanId, Guid besinId)
    {
        var planId = await TaslakPlanKurAsync(diyetisyen, danisanId, besinId);
        var yayim = await diyetisyen.PostAsync($"/api/plans/{planId}/publish", null, Iptal);
        Assert.Equal(HttpStatusCode.OK, yayim.StatusCode);
        return planId;
    }

    /// <summary>Bugünün tarihi (Türkiye saatiyle).</summary>
    public static DateOnly Bugun() => DateOnly.FromDateTime(DateTime.UtcNow.AddHours(3));
}

internal sealed record DanisanOturumu(Guid Id, HttpClient Http);

internal sealed record KimlikYaniti(Guid Id);

internal sealed record Toplamlar(decimal EnergyKcal, decimal ProteinG, decimal CarbsG, decimal FatG,
    decimal ProteinPercent, decimal CarbsPercent, decimal FatPercent);

internal sealed record PlanSonucu(Guid PlanId, string Status, Guid? SourceTemplateId, Toplamlar Totals, List<string> Warnings);

internal sealed record BildirimSatiri(Guid Id, string Type, string Title, string Body, bool IsRead, DateTime CreatedAt);

internal sealed record BildirimSayfasi(List<BildirimSatiri> Items, int Page, int PageSize, int TotalCount);
