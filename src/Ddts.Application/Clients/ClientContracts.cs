using Ddts.Domain.Clients;

namespace Ddts.Application.Clients;

/// <summary>Danışan ekleme formu (FR-B1; docs/api.md 8.3, örnek 2).</summary>
public sealed record AddClientCommand(
    string FullName,
    string Email,
    string? Phone,
    DateOnly BirthDate,
    Sex Sex,
    GoalType GoalType,
    decimal? TargetWeightKg,
    string? Preferences,
    string? AllergyNotes,
    bool KvkkConsentGiven);

/// <summary>Danışan listesindeki satır.</summary>
public sealed record ClientSummaryDto(Guid Id, string FullName, string? Phone, GoalType GoalType, bool IsActive);

/// <summary>Kısıt listesindeki besin: Allergy → plana eklenemez, Dislike → uyarı (FR-B2).</summary>
public sealed record ClientRestrictionDto(Guid FoodItemId, string FoodName, RestrictionType Type);

/// <summary>Danışan profili (diyetisyen paneli ve danışanın kendi profili).</summary>
public sealed record ClientDetailDto(
    Guid Id,
    string FullName,
    string Email,
    string? Phone,
    DateOnly BirthDate,
    Sex Sex,
    GoalType GoalType,
    decimal? TargetWeightKg,
    string? Preferences,
    string? AllergyNotes,
    bool IsActive,
    DateTime KvkkConsentAt,
    IReadOnlyList<ClientRestrictionDto> Restrictions);

/// <summary>Kısıt listesine yazılacak besin ve türü.</summary>
public sealed record RestrictionInput(Guid FoodItemId, RestrictionType Type);

/// <summary>Değişiklik geçmişindeki bir alan: eski ve yeni değer (FR-B3).</summary>
public sealed record FieldChangeDto(string Field, string? OldValue, string? NewValue);

/// <summary>Değişiklik geçmişi satırı: ne zaman, hangi işlem, kim yaptı ve neler değişti (FR-B3).</summary>
public sealed record ClientHistoryEntryDto(DateTime At, string Action, Guid? UserId, IReadOnlyList<FieldChangeDto> Changes);
