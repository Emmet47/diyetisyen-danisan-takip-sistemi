namespace Ddts.Application.MealLogs;

/// <summary>Gelen port: öğün günlüğü ve fotoğraf (G modülü, FR-G1–G3, Şekil 5.5).</summary>
public interface IMealLogService
{
    /// <summary>Danışanın bugünkü planlı öğünleri ve kayıt durumları (/api/me/meals/today).</summary>
    Task<TodayDto> GetTodayAsync(CancellationToken cancellationToken = default);

    /// <summary>Danışan öğünü işaretler; aynı öğün aynı gün için bir kez kaydedilir (BR-11, 409).</summary>
    Task<Guid> LogMealAsync(LogMealCommand command, CancellationToken cancellationToken = default);

    Task UpdateLogAsync(Guid mealLogId, UpdateMealLogCommand command, CancellationToken cancellationToken = default);

    /// <summary>Danışanın kayıtları (/api/me/meal-logs?from=&amp;to=).</summary>
    Task<IReadOnlyList<MealLogDto>> ListMyLogsAsync(DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default);

    /// <summary>Fotoğraf ekler: yalnızca JPEG/PNG (dosya imzasına bakılır), en fazla 5 MB, kayıt başına bir tane (BR-10).</summary>
    Task AttachPhotoAsync(Guid mealLogId, Stream content, CancellationToken cancellationToken = default);

    /// <summary>10 dakika geçerli fotoğraf bağlantısı; danışan kendi, diyetisyen kendi danışanının kaydı için.</summary>
    Task<PhotoUrlDto> GetPhotoUrlAsync(Guid mealLogId, CancellationToken cancellationToken = default);

    /// <summary>Diyetisyen için planlanan–gerçekleşen öğünler (FR-G3), en fazla 31 gün.</summary>
    Task<MealComparisonDto> GetComparisonAsync(Guid clientId, DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default);
}
