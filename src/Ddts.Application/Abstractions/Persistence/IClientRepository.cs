using Ddts.Domain.Clients;

namespace Ddts.Application.Abstractions.Persistence;

/// <summary>
/// Danışanlar (Şekil 4.10). Diyetisyen verisi okuyan her metot dietitianId alır:
/// başka diyetisyenin kaydı hiç dönmez (IDOR koruması, NFR-G1).
/// </summary>
public interface IClientRepository
{
    /// <summary>Danışanı alerji/kısıt listesiyle birlikte getirir; başka diyetisyenin danışanıysa null.</summary>
    Task<Client?> GetAsync(Guid dietitianId, Guid clientId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Client>> ListAsync(Guid dietitianId, bool onlyActive, CancellationToken cancellationToken = default);

    /// <summary>Paket limitine sayılan aktif danışan sayısı (BR-01).</summary>
    Task<int> CountActiveAsync(Guid dietitianId, CancellationToken cancellationToken = default);

    void Add(Client client);

    void Remove(Client client);
}
