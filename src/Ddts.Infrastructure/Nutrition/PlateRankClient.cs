using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace Ddts.Infrastructure.Nutrition;

/// <summary>
/// PlateRank REST istemcisi (Adaptee, Şekil 4.9). Uç salt-okunurdur ve PostgREST sorgu biçimini kullanır
/// (ad=ilike.*mercimek*). Anahtar "apikey" başlığıyla gider, adrese yazılmaz; istemcilere hiç gitmez (NFR-G5).
/// HttpClient zaman aşımı Program.cs'te verilir (3 sn).
/// Lisans (platerank.dev/lisans): PlateRank'in kendi verisi CC BY 4.0, atıfla kullanılır. McDonald's satırları
/// bu lisansın dışında, Open Food Facts satırları ise ODbL (aynı lisansla paylaşma şartı) olduğu için sorguda
/// hiç istenmez.
/// </summary>
public sealed class PlateRankClient(HttpClient httpClient, IOptions<PlateRankOptions> options)
{
    private const string Columns = "id,ad,mutfak,kcal,protein,karbonhidrat,yag,porsiyon_g,kaynak,updated_at";

    // Yayındaki satırlar; kaynağı boş (PlateRank'in kendi verisi) ya da lisansı uygun olanlar.
    private const string LicenseFilter =
        "yayinda=eq.true&or=(kaynak.is.null,and(kaynak.not.ilike.*mcdonald*,kaynak.not.ilike.*open*food*facts*))";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(options.Value.ApiKey);

    public async Task<IReadOnlyList<PlateRankDishDto>> SearchDishesAsync(string query, int limit, CancellationToken cancellationToken)
    {
        // "*" PostgREST'te joker karakter; kullanıcı girdisinde joker sayılmasın diye atılır.
        var text = query.Replace("*", " ").Replace("%", " ").Trim();
        var url = $"yemekler?select={Columns}&{LicenseFilter}&ad={Uri.EscapeDataString($"ilike.*{text}*")}&order=ad.asc&limit={limit}";
        return await GetAsync(url, cancellationToken);
    }

    /// <summary>Tek yemek; PlateRank'te yoksa (ya da lisansı uygun değilse) null.</summary>
    public async Task<PlateRankDishDto?> GetDishAsync(Guid id, CancellationToken cancellationToken)
    {
        var url = $"yemekler?select={Columns}&{LicenseFilter}&id=eq.{id:D}&limit=1";
        var dishes = await GetAsync(url, cancellationToken);
        return dishes.Count == 0 ? null : dishes[0];
    }

    private async Task<IReadOnlyList<PlateRankDishDto>> GetAsync(string relativeUrl, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, relativeUrl);
        request.Headers.Add("apikey", options.Value.ApiKey);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<List<PlateRankDishDto>>(cancellationToken) ?? [];
    }
}
