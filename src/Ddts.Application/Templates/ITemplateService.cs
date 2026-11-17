using Ddts.Application.Plans;

namespace Ddts.Application.Templates;

/// <summary>Gelen port: plan şablonları (F modülü, FR-F1–F3, Şekil 4.8 ve 5.3).</summary>
public interface ITemplateService
{
    Task<Guid> CreateAsync(TemplateCommand command, CancellationToken cancellationToken = default);

    Task UpdateAsync(Guid templateId, TemplateCommand command, CancellationToken cancellationToken = default);

    Task DeleteAsync(Guid templateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Prototype: şablonun bağımsız (derin) kopyasını danışana taslak plan olarak oluşturur. Danışanın kısıtlarına
    /// takılan besinler uyarı olarak döner; kopyada yapılan değişiklik şablonu etkilemez (BR-07).
    /// </summary>
    Task<PlanCreatedDto> CopyToClientAsync(Guid templateId, Guid clientId, string? title, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TemplateListItemDto>> ListAsync(CancellationToken cancellationToken = default);

    Task<TemplateSummaryDto> GetAsync(Guid templateId, CancellationToken cancellationToken = default);
}
