using System.Globalization;
using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Ddts.Infrastructure.Nutrition;

/// <summary>
/// USDA FoodData Central REST istemcisi (Adaptee, Şekil 4.9). API anahtarı sunucuda X-Api-Key başlığıyla
/// gönderilir (adreste olmadığı için loglara da düşmez); istemcilere (web/mobil) hiç gitmez (NFR-G5). HttpClient zaman aşımı Program.cs'te 3 sn olarak verilir.
/// JSON elle okunur: arama ve tek besin uçlarının besin listesi biçimleri farklıdır (nutrientId/value ve
/// nutrient.id/amount); ikisi de aynı FdcFoodDto'ya çevrilir.
/// </summary>
public sealed class UsdaFdcClient(HttpClient httpClient, IOptions<UsdaOptions> options)
{
    // Yalnızca genel besin verisi: markalı ürünler (Branded) aramayı kalabalıklaştırır.
    private const string DataTypes = "Foundation,SR%20Legacy";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(options.Value.ApiKey);

    public async Task<IReadOnlyList<FdcFoodDto>> SearchFoodsAsync(string query, int pageSize, CancellationToken cancellationToken)
    {
        var url = $"foods/search?query={Uri.EscapeDataString(query)}&pageSize={pageSize}&dataType={DataTypes}";
        using var response = await SendAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        if (!json.RootElement.TryGetProperty("foods", out var foods) || foods.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        return foods.EnumerateArray().Select(ReadFood).OfType<FdcFoodDto>().ToList();
    }

    /// <summary>Tek besin; USDA'da yoksa null.</summary>
    public async Task<FdcFoodDto?> GetFoodAsync(int fdcId, CancellationToken cancellationToken)
    {
        using var response = await SendAsync($"food/{fdcId}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var json = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        return ReadFood(json.RootElement);
    }

    private async Task<HttpResponseMessage> SendAsync(string relativeUrl, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, relativeUrl);
        request.Headers.Add("X-Api-Key", options.Value.ApiKey);
        return await httpClient.SendAsync(request, cancellationToken);
    }

    private static FdcFoodDto? ReadFood(JsonElement food)
    {
        if (ReadInt(food, "fdcId") is not { } fdcId)
        {
            return null;
        }

        var description = food.TryGetProperty("description", out var d) && d.ValueKind == JsonValueKind.String
            ? d.GetString() ?? string.Empty
            : string.Empty;

        var nutrients = new List<FdcNutrientDto>();
        if (food.TryGetProperty("foodNutrients", out var list) && list.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in list.EnumerateArray())
            {
                // Arama yanıtı: { nutrientId, nutrientNumber, unitName, value }
                // Tek besin yanıtı: { nutrient: { id, number, unitName }, amount }
                var inner = item.TryGetProperty("nutrient", out var n) && n.ValueKind == JsonValueKind.Object ? n : item;

                nutrients.Add(new FdcNutrientDto(
                    ReadInt(item, "nutrientId") ?? ReadInt(inner, "id"),
                    ReadString(item, "nutrientNumber") ?? ReadString(inner, "number"),
                    ReadString(item, "unitName") ?? ReadString(inner, "unitName"),
                    ReadDecimal(item, "value") ?? ReadDecimal(item, "amount")));
            }
        }

        return new FdcFoodDto(fdcId, description, nutrients);
    }

    private static int? ReadInt(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) switch
        {
            true when value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number) => number,
            true when value.ValueKind == JsonValueKind.String
                && int.TryParse(value.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => null,
        };

    private static decimal? ReadDecimal(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number)
            ? number
            : null;

    private static string? ReadString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) switch
        {
            true when value.ValueKind == JsonValueKind.String => value.GetString(),
            true when value.ValueKind == JsonValueKind.Number => value.GetRawText(),
            _ => null,
        };
}
