using Ddts.Domain.Common;
using Ddts.Domain.Notifications;

namespace Ddts.Domain.Clients;

/// <summary>
/// Danışan profili (Şekil 4.1). Her danışan tek bir diyetisyene bağlıdır (DietitianId = tenant).
/// Açık rıza (KVKK) alınmadan danışan oluşturulamaz.
/// </summary>
public sealed class Client
{
    private readonly List<FoodRestriction> _restrictions = [];

    private Client()
    {
    }

    public Client(
        Guid dietitianId,
        Guid userId,
        ClientProfile profile,
        DateOnly birthDate,
        Sex sex,
        DateTime kvkkConsentAt)
    {
        Id = Guid.CreateVersion7();
        DietitianId = dietitianId;
        UserId = userId;
        BirthDate = birthDate;
        Sex = sex;
        KvkkConsentAt = Guard.Utc(kvkkConsentAt, nameof(kvkkConsentAt));
        IsActive = true;
        Notifications = NotificationSettings.AllEnabled;
        ApplyProfile(profile);
    }

    public Guid Id { get; private set; }

    /// <summary>Bağlı olduğu diyetisyen. Bütün sorgular bu alanla süzülür (NFR-G1).</summary>
    public Guid DietitianId { get; private set; }

    /// <summary>Danışanın giriş hesabı (users tablosu, rol = Client).</summary>
    public Guid UserId { get; private set; }

    public string FullName { get; private set; } = null!;

    public string? Phone { get; private set; }

    public DateOnly BirthDate { get; private set; }

    public Sex Sex { get; private set; }

    public GoalType GoalType { get; private set; }

    public decimal? TargetWeightKg { get; private set; }

    /// <summary>Beslenme tercihleri (serbest metin).</summary>
    public string? Preferences { get; private set; }

    /// <summary>Alerji notu (serbest metin). Plana eklenmesi yasak besinler ayrıca Restrictions listesinde.</summary>
    public string? AllergyNotes { get; private set; }

    /// <summary>Pasif danışan paket sayımına girmez ve giriş yapamaz (FR-B4).</summary>
    public bool IsActive { get; private set; }

    /// <summary>Açık rızanın alındığı an (UTC).</summary>
    public DateTime KvkkConsentAt { get; private set; }

    public NotificationSettings Notifications { get; private set; } = null!;

    public IReadOnlyCollection<FoodRestriction> Restrictions => _restrictions.AsReadOnly();

    /// <summary>
    /// Profil alanlarını günceller. Eski/yeni değerler servis katmanında denetim kaydına
    /// (audit_logs.details) yazılır (FR-B3).
    /// </summary>
    public void UpdateProfile(ClientProfile changes) => ApplyProfile(changes);

    public void Deactivate() => IsActive = false;

    /// <summary>Verilen tarihteki yaş (enerji formülleri için).</summary>
    public int AgeOn(DateOnly date)
    {
        var age = date.Year - BirthDate.Year;

        // Bu yılki doğum günü henüz gelmediyse bir eksik
        if (date < BirthDate.AddYears(age))
        {
            age--;
        }

        return age;
    }

    /// <summary>Besin danışanın listesinde varsa kısıt türünü, yoksa null döner (BR-05).</summary>
    public RestrictionType? GetRestriction(Guid foodItemId) =>
        _restrictions.FirstOrDefault(r => r.FoodItemId == foodItemId)?.Type;

    /// <summary>Danışan bu türdeki bildirimi açık tutuyor mu? (FR-J2, BR-12)</summary>
    public bool AllowsNotification(NotificationType type) => type switch
    {
        NotificationType.AppointmentRequested or
        NotificationType.AppointmentConfirmed or
        NotificationType.AppointmentCancelled or
        NotificationType.AppointmentRescheduled or
        NotificationType.AppointmentReminder => Notifications.Appointments,
        NotificationType.MealLogReminder => Notifications.MealReminders,
        NotificationType.MeasurementReminder => Notifications.Measurements,
        NotificationType.PlanPublished => Notifications.Plans,
        _ => false,
    };

    private void ApplyProfile(ClientProfile profile)
    {
        FullName = Guard.Required(profile.FullName, 100, "Ad soyad");
        Phone = Guard.Optional(profile.Phone, 20, "Telefon");
        GoalType = profile.GoalType;
        TargetWeightKg = Guard.InRange(profile.TargetWeightKg, 20m, 350m, "Hedef kilo");
        Preferences = Guard.Optional(profile.Preferences, 500, "Beslenme tercihleri");
        AllergyNotes = Guard.Optional(profile.AllergyNotes, 500, "Alerji notu");
    }
}
