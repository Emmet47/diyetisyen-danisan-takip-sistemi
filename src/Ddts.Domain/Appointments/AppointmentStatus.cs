namespace Ddts.Domain.Appointments;

/// <summary>
/// Randevu durumu (BR-09). Requested ve Confirmed "aktif" sayılır:
/// veritabanındaki EXCLUDE kısıtı yalnızca bu ikisinin çakışmasını engeller.
/// </summary>
public enum AppointmentStatus
{
    Requested,
    Confirmed,
    Cancelled,
    Completed,
}
