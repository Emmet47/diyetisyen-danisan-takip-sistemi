using Ddts.Domain.Plans;

namespace Ddts.Application.Abstractions.Persistence;

/// <summary>Plan şablonları (Şekil 4.10, FR-F1). Yalnızca sahibi diyetisyen görür.</summary>
public interface IPlanTemplateRepository
{
    /// <summary>Şablonu öğünleri, satırları ve besinleriyle getirir; başka diyetisyeninse null.</summary>
    Task<PlanTemplate?> GetAsync(Guid dietitianId, Guid templateId, CancellationToken cancellationToken = default);

    /// <summary>Diyetisyenin şablonları ada göre sıralı (öğünsüz özet).</summary>
    Task<IReadOnlyList<PlanTemplate>> ListAsync(Guid dietitianId, CancellationToken cancellationToken = default);

    void Add(PlanTemplate template);

    void Remove(PlanTemplate template);

    /// <summary>Şablon düzenlenince yerine yenisi gelen eski öğünleri (satırlarıyla) siler.</summary>
    void RemoveMeals(IEnumerable<Meal> meals);
}
