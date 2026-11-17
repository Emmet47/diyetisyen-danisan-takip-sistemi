using Ddts.Application.Plans;
using Ddts.Domain.MealLogs;

namespace Ddts.Application.MealLogs;

/// <summary>Öğün kaydı girdisi (FR-G1): yayımlanmış plandaki öğün, gün (bugün ya da geçmiş), durum ve not.</summary>
public sealed record LogMealCommand(Guid MealId, DateOnly LogDate, MealLogStatus Status, string? Note);

/// <summary>Öğün kaydını güncelleme: durum ve not.</summary>
public sealed record UpdateMealLogCommand(MealLogStatus Status, string? Note);

public sealed record MealLogDto(
    Guid Id,
    Guid MealId,
    DateOnly LogDate,
    MealLogStatus Status,
    string? Note,
    bool HasPhoto,
    DateTime LoggedAt);

/// <summary>Bugünün planlı öğünü ve (varsa) kaydı.</summary>
public sealed record TodayMealDto(MealDto Meal, MealLogDto? Log);

/// <summary>Mobil "Bugün" ekranı (FR-E5, Şekil 5.5). Yayımlanmış plan yoksa PlanId boş, liste boş.</summary>
public sealed record TodayDto(DateOnly Date, Guid? PlanId, string? PlanTitle, IReadOnlyList<TodayMealDto> Meals);

/// <summary>Planlanan öğün ve o gün için gerçekleşen kayıt (yoksa Log boş: kaydedilmemiş).</summary>
public sealed record ComparisonMealDto(Guid MealId, string Name, TimeOnly Time, MealLogDto? Log);

public sealed record ComparisonDayDto(DateOnly Date, IReadOnlyList<ComparisonMealDto> Meals);

/// <summary>Planlanan–gerçekleşen öğünler, gün gün (FR-G3).</summary>
public sealed record MealComparisonDto(DateOnly From, DateOnly To, IReadOnlyList<ComparisonDayDto> Days);

/// <summary>10 dakika geçerli, imzalı fotoğraf bağlantısı (FR-G2).</summary>
public sealed record PhotoUrlDto(string Url, DateTime ExpiresAt);
