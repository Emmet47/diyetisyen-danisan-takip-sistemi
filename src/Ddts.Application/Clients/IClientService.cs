using Ddts.Domain.Clients;

namespace Ddts.Application.Clients;

/// <summary>Gelen port: danışan profil yönetimi (B modülü, FR-B1–B4). Uygulaması ClientService.</summary>
public interface IClientService
{
    /// <summary>Paket limitini kontrol ederek danışan ekler ve davet e-postası gönderir (Şekil 5.1).</summary>
    Task<Guid> AddClientAsync(AddClientCommand command, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ClientSummaryDto>> ListAsync(bool onlyActive, CancellationToken cancellationToken = default);

    Task<ClientDetailDto> GetAsync(Guid clientId, CancellationToken cancellationToken = default);

    /// <summary>Profili günceller; değişen alanlar eski/yeni değerleriyle geçmişe yazılır (FR-B3).</summary>
    Task UpdateClientAsync(Guid clientId, ClientProfile profile, CancellationToken cancellationToken = default);

    Task SetRestrictionsAsync(Guid clientId, IReadOnlyList<RestrictionInput> restrictions, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ClientHistoryEntryDto>> GetHistoryAsync(Guid clientId, CancellationToken cancellationToken = default);

    /// <summary>Pasife alır: paket sayımına girmez, danışan giriş yapamaz, veriler durur (FR-B4).</summary>
    Task DeactivateAsync(Guid clientId, CancellationToken cancellationToken = default);

    /// <summary>Paket limitini yeniden kontrol ederek aktifleştirir (FR-B4, FR-L2).</summary>
    Task ActivateAsync(Guid clientId, CancellationToken cancellationToken = default);

    /// <summary>Danışanı ve bütün kayıtlarını kalıcı siler (FR-B4, belge 2.5 madde 4).</summary>
    Task DeleteAsync(Guid clientId, CancellationToken cancellationToken = default);

    /// <summary>Giriş yapmış danışanın kendi profili (/api/me/profile).</summary>
    Task<ClientDetailDto> GetMyProfileAsync(CancellationToken cancellationToken = default);
}
