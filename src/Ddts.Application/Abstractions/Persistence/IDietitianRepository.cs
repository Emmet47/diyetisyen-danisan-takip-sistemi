using Ddts.Domain.Dietitians;

namespace Ddts.Application.Abstractions.Persistence;

/// <summary>Diyetisyen kayıtları. Repository deseni, Şekil 4.10.</summary>
public interface IDietitianRepository
{
    Task<Dietitian?> GetAsync(Guid dietitianId, CancellationToken cancellationToken = default);

    void Add(Dietitian dietitian);
}
