namespace Ddts.Domain.Clients;

/// <summary>
/// Danışanın dört bildirim izni (değer nesnesi, Şekil 4.3; FR-J2).
/// Veritabanında clients tablosunun notify_appointments, notify_meal_reminders,
/// notify_measurements, notify_plans sütunları.
/// </summary>
public sealed record NotificationSettings(bool Appointments, bool MealReminders, bool Measurements, bool Plans)
{
    /// <summary>Yeni danışanda hepsi açık başlar (şemadaki DEFAULT true ile aynı).</summary>
    public static NotificationSettings AllEnabled { get; } = new(true, true, true, true);
}
