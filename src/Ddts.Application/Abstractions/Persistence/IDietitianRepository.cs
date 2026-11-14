using Ddts.Domain.Dietitians;

namespace Ddts.Application.Abstractions.Persistence;

/// <summary>Diyetisyen kayıtları. Repository deseni, Şekil 4.10.</summary>
public interface IDietitianRepository
{
    Task<Dietitian?> GetAsync(Guid dietitianId, CancellationToken cancellationToken = default);

    /// <summary>Yönetici listesi için bir sayfa diyetisyen, ada göre sıralı (sayfa 1'den başlar).</summary>
    Task<IReadOnlyList<Dietitian>> ListAsync(int page, int pageSize, CancellationToken cancellationToken = default);

    Task<int> CountAsync(CancellationToken cancellationToken = default);

    void Add(Dietitian dietitian);
}
