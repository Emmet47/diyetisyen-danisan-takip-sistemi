using Ddts.Domain.Measurements;

namespace Ddts.Application.Abstractions.Persistence;

/// <summary>
/// Danışan ölçümleri (Şekil 4.10). Ölçüm tablosunda diyetisyen sütunu yok: servis önce danışanın
/// giriş yapmış diyetisyene ait olduğunu IClientRepository.GetAsync(dietitianId, clientId) ile doğrular,
/// sonra bu metotları o danışanın kimliğiyle çağırır (IDOR koruması, NFR-G1).
/// </summary>
public interface IMeasurementRepository
{
    /// <summary>Tarih aralığındaki ölçümler (iki uç dahil), eskiden yeniye sıralı.</summary>
    Task<IReadOnlyList<Measurement>> ListAsync(Guid clientId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);

    /// <summary>Danışanın ölçümü; başka danışanınsa null.</summary>
    Task<Measurement?> GetAsync(Guid clientId, Guid measurementId, CancellationToken cancellationToken = default);

    void Add(Measurement measurement);

    void Remove(Measurement measurement);
}
