namespace Ddts.Domain.Clients;

/// <summary>
/// Diyetisyenin değiştirebildiği profil alanları (değer nesnesi, Şekil 4.3).
/// Client.UpdateProfile bu nesneyi alır; doğum tarihi ve cinsiyet buraya dahil değil.
/// </summary>
public sealed record ClientProfile(
    string FullName,
    string? Phone,
    GoalType GoalType,
    decimal? TargetWeightKg,
    string? Preferences,
    string? AllergyNotes);
