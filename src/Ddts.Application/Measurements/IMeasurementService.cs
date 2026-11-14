namespace Ddts.Application.Measurements;

/// <summary>Gelen port: ölçüm ve gelişim takibi (C modülü, FR-C1–C4). Uygulaması MeasurementService.</summary>
public interface IMeasurementService
{
    /// <summary>Diyetisyen danışanına ölçüm ekler; o gün için ölçüm varsa 409 (Şekil 5.6).</summary>
    Task<Guid> AddAsync(Guid clientId, MeasurementCommand command, CancellationToken cancellationToken = default);

    /// <summary>Yanlış girilmiş ölçümü düzeltir (Senaryo 4, adım 1a).</summary>
    Task UpdateAsync(Guid clientId, Guid measurementId, MeasurementCommand command, CancellationToken cancellationToken = default);

    /// <summary>Ölçüm geçmişi, önceki ölçüme farklar ve hedefe kalan kilo (FR-C2, FR-C3).</summary>
    Task<MeasurementHistoryDto> ListAsync(Guid clientId, CancellationToken cancellationToken = default);

    /// <summary>Ölçümü siler (docs/api.md 8.2: DELETE /api/clients/{id}/measurements/{measurementId}).</summary>
    Task DeleteAsync(Guid clientId, Guid measurementId, CancellationToken cancellationToken = default);

    /// <summary>Giriş yapmış danışanın kendi ölçüm geçmişi (/api/me/measurements, FR-C4).</summary>
    Task<MeasurementHistoryDto> ListMyMeasurementsAsync(CancellationToken cancellationToken = default);
}
