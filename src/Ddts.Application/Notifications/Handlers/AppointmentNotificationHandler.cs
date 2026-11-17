using Ddts.Application.Abstractions.Events;
using Ddts.Application.Abstractions.Persistence;
using Ddts.Application.Common;
using Ddts.Domain.Appointments;
using Ddts.Domain.Notifications;
using Ddts.Domain.Users;

namespace Ddts.Application.Notifications.Handlers;

/// <summary>
/// Observer (Şekil 4.11, 5.7): randevu olaylarında karşı tarafa bildirim (FR-J1). Talep → diyetisyene;
/// onay → danışana; iptal ve erteleme → işlemi yapmayan tarafa. Metinde yalnızca tarih ve saat var.
/// </summary>
public sealed class AppointmentNotificationHandler(
    IAppointmentRepository appointments,
    IDietitianRepository dietitians,
    IClientRepository clients,
    INotificationService notifications) : IDomainEventHandler<AppointmentEvent>
{
    public async Task HandleAsync(AppointmentEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var appointment = await appointments.GetAsync(domainEvent.DietitianId, domainEvent.AppointmentId, cancellationToken);
        if (appointment is null)
        {
            return;
        }

        var when = LocalTime.Format(appointment.StartsAt);

        var (to, type, title, body) = domainEvent switch
        {
            AppointmentRequested => (UserRole.Dietitian, NotificationType.AppointmentRequested,
                "Yeni randevu talebi", $"{when} için yeni bir randevu talebin var."),
            AppointmentConfirmed => (UserRole.Client, NotificationType.AppointmentConfirmed,
                "Randevun onaylandı", $"{when} randevun onaylandı."),
            AppointmentCancelled cancelled => (Other(cancelled.By), NotificationType.AppointmentCancelled,
                "Randevu iptal edildi", $"{when} randevusu iptal edildi."),
            AppointmentRescheduled rescheduled => (Other(rescheduled.By), NotificationType.AppointmentRescheduled,
                "Randevu ertelendi", rescheduled.By == UserRole.Client
                    ? $"Danışanın randevuyu {when} saatine erteledi, onayını bekliyor."
                    : $"Randevun {when} saatine ertelendi."),
            _ => (UserRole.Admin, NotificationType.AppointmentRequested, string.Empty, string.Empty),
        };

        if (to == UserRole.Admin)
        {
            return;
        }

        Guid? userId = to == UserRole.Dietitian
            ? (await dietitians.GetAsync(domainEvent.DietitianId, cancellationToken))?.UserId
            : (await clients.GetAsync(domainEvent.DietitianId, domainEvent.ClientId, cancellationToken))?.UserId;

        if (userId is { } id)
        {
            await notifications.NotifyAsync(id, type, title, body, dedupKey: null, cancellationToken);
        }
    }

    private static UserRole Other(UserRole by) => by == UserRole.Client ? UserRole.Dietitian : UserRole.Client;
}
