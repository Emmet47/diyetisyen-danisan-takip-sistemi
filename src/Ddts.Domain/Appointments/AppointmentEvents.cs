using Ddts.Domain.Common;
using Ddts.Domain.Users;

namespace Ddts.Domain.Appointments;

/// <summary>
/// Randevu olaylarının ortak tabanı (Şekil 4.11). AppointmentNotificationHandler hepsini dinler
/// ve ilgili tarafa bildirim üretir (FR-J1).
/// </summary>
public abstract record AppointmentEvent(Guid AppointmentId, Guid DietitianId, Guid ClientId, DateTime OccurredAt)
    : IDomainEvent;

/// <summary>Danışan randevu talep etti → diyetisyene bildirim.</summary>
public sealed record AppointmentRequested(Guid AppointmentId, Guid DietitianId, Guid ClientId, DateTime OccurredAt)
    : AppointmentEvent(AppointmentId, DietitianId, ClientId, OccurredAt);

/// <summary>Randevu onaylandı (ya da diyetisyen doğrudan onaylı oluşturdu) → danışana bildirim.</summary>
public sealed record AppointmentConfirmed(Guid AppointmentId, Guid DietitianId, Guid ClientId, DateTime OccurredAt)
    : AppointmentEvent(AppointmentId, DietitianId, ClientId, OccurredAt);

/// <summary>Randevu iptal edildi → karşı tarafa bildirim (By: iptal eden taraf).</summary>
public sealed record AppointmentCancelled(Guid AppointmentId, Guid DietitianId, Guid ClientId, DateTime OccurredAt, UserRole By)
    : AppointmentEvent(AppointmentId, DietitianId, ClientId, OccurredAt);

/// <summary>Randevu ertelendi → karşı tarafa bildirim (By: erteleyen taraf).</summary>
public sealed record AppointmentRescheduled(Guid AppointmentId, Guid DietitianId, Guid ClientId, DateTime OccurredAt, UserRole By)
    : AppointmentEvent(AppointmentId, DietitianId, ClientId, OccurredAt);
