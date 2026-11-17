using Ddts.Domain.MealLogs;

namespace Ddts.Application.Abstractions.Persistence;

/// <summary>
/// Öğün kayıtları (Şekil 4.10, FR-G1). Tabloda diyetisyen sütunu yok: servis önce danışanın sahipliğini
/// IClientRepository ile doğrular (diyetisyen) ya da kaydın token'daki danışana ait olduğuna bakar (danışan).
/// </summary>
public interface IMealLogRepository
{
    /// <summary>Tarih aralığındaki kayıtlar (iki uç dahil), eskiden yeniye.</summary>
    Task<IReadOnlyList<MealLog>> ListAsync(Guid clientId, DateOnly from, DateOnly to, CancellationToken cancellationToken = default);

    Task<MealLog?> GetAsync(Guid mealLogId, CancellationToken cancellationToken = default);

    /// <summary>Danışanın depodaki fotoğraf anahtarları (kalıcı silmede depodan da silinir, belge 2.5 madde 4).</summary>
    Task<IReadOnlyList<string>> ListPhotoKeysAsync(Guid clientId, CancellationToken cancellationToken = default);

    void Add(MealLog log);
}
