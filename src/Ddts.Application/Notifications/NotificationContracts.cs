using Ddts.Domain.Notifications;

namespace Ddts.Application.Notifications;

public sealed record NotificationDto(Guid Id, NotificationType Type, string Title, string Body, bool IsRead, DateTime CreatedAt);

/// <summary>Danışanın bildirim izinleri (FR-J2): randevu, öğün, ölçüm ve plan bildirimleri.</summary>
public sealed record NotificationSettingsDto(bool Appointments, bool MealReminders, bool Measurements, bool Plans);
