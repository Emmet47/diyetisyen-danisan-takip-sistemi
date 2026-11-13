namespace Ddts.Domain.Notifications;

/// <summary>Bildirim türleri (FR-J1). notifications.type sütunundaki CHECK listesiyle aynı.</summary>
public enum NotificationType
{
    AppointmentRequested,
    AppointmentConfirmed,
    AppointmentCancelled,
    AppointmentRescheduled,
    AppointmentReminder,
    PlanPublished,
    MealLogReminder,
    MeasurementReminder,
}
