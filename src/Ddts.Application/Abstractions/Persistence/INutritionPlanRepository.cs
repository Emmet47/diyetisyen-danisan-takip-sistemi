using Ddts.Domain.Plans;

namespace Ddts.Application.Abstractions.Persistence;

/// <summary>
/// Beslenme planları (Şekil 4.10). Planlar öğünleri, satırları ve satırların besinleriyle birlikte yüklenir
/// (Composite toplamları için). Diyetisyen verisi okuyan metotlar dietitianId alır (IDOR koruması).
/// </summary>
public interface INutritionPlanRepository
{
    Task<NutritionPlan?> GetAsync(Guid dietitianId, Guid planId, CancellationToken cancellationToken = default);

    /// <summary>Danışanın yayımlanmış planı (BR-03: en fazla bir tane); yoksa null.</summary>
    Task<NutritionPlan?> GetPublishedForClientAsync(Guid clientId, CancellationToken cancellationToken = default);

    /// <summary>Diyetisyenin planları, en yenisi önce; clientId verilirse yalnızca o danışanınkiler (öğünsüz özet).</summary>
    Task<IReadOnlyList<NutritionPlan>> ListAsync(Guid dietitianId, Guid? clientId, CancellationToken cancellationToken = default);

    /// <summary>Danışanın yayımlanmış ve arşivlenmiş planları öğünleriyle (planlanan–gerçekleşen karşılaştırması, rapor).</summary>
    Task<IReadOnlyList<NutritionPlan>> ListForClientAsync(Guid clientId, CancellationToken cancellationToken = default);

    /// <summary>Bütün yayımlanmış planlar öğünleriyle (ReminderWorker'ın öğün hatırlatması için).</summary>
    Task<IReadOnlyList<NutritionPlan>> ListPublishedAsync(CancellationToken cancellationToken = default);

    void Add(NutritionPlan plan);

    void Remove(NutritionPlan plan);

    /// <summary>Taslak plan düzenlenince yerine yenisi gelen eski öğünleri (satırlarıyla) siler.</summary>
    void RemoveMeals(IEnumerable<Meal> meals);
}
