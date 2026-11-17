using Ddts.Application.Abstractions.Notifications;
using Ddts.Application.Abstractions.Persistence;
using Ddts.Application.Abstractions.Security;
using Ddts.Application.Common;
using Ddts.Domain.Clients;
using Ddts.Domain.Common;
using Ddts.Domain.Notifications;
using Ddts.Domain.Users;

namespace Ddts.Application.Notifications;

/// <summary>
/// Bildirimler (J modülü). Observer handler'ları ve ReminderService bildirimleri NotifyAsync ile üretir.
/// İzinli bildirim (FR-J2): danışanın kapattığı türde bildirim üretilmez. Metinlerde sağlık verisi yoktur
/// (KVKK, belge 2.5 madde 3). Push gönderilemezse bildirim uygulama içinde yine görünür.
/// </summary>
public sealed class NotificationService(
    INotificationRepository notifications,
    IUserRepository users,
    IClientRepository clients,
    IPushNotificationSender pushSender,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    TimeProvider timeProvider) : INotificationService
{
    public const int MaxPushTokenLength = 300;

    public async Task NotifyAsync(
        Guid userId, NotificationType type, string title, string body, string? dedupKey, CancellationToken cancellationToken = default)
    {
        if (dedupKey is not null && await notifications.DedupKeyExistsAsync(dedupKey, cancellationToken))
        {
            return;
        }

        var user = await users.GetAsync(userId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            return;
        }

        if (user.Role == UserRole.Client
            && await clients.GetByUserIdAsync(userId, cancellationToken) is { } client
            && !client.AllowsNotification(type))
        {
            return;
        }

        var notification = new Notification(userId, type, title, body, dedupKey);
        notifications.Add(notification);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(user.PushToken))
        {
            return;
        }

        try
        {
            await pushSender.SendAsync(user.PushToken, title, body, cancellationToken);
            notification.MarkSent(timeProvider.GetUtcNow().UtcDateTime);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            // Push gitmediyse (adapter hatayı logladı) bildirim uygulama içi listede kalır; SentAt boş kalır.
        }
    }

    public async Task<PagedResult<NotificationDto>> ListAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        Paging.Ensure(page, pageSize);
        var userId = currentUser.UserId;

        var items = await notifications.ListForUserAsync(userId, page, pageSize, cancellationToken);
        var total = await notifications.CountForUserAsync(userId, cancellationToken);

        return new PagedResult<NotificationDto>(
            items.Select(n => new NotificationDto(n.Id, n.Type, n.Title, n.Body, n.IsRead, n.CreatedAt)).ToList(),
            page,
            pageSize,
            total);
    }

    public async Task MarkReadAsync(Guid notificationId, CancellationToken cancellationToken = default)
    {
        var notification = await notifications.GetAsync(currentUser.UserId, notificationId, cancellationToken)
            ?? throw new NotFoundException("Bildirim bulunamadı.");

        notification.MarkRead();
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateSettingsAsync(NotificationSettingsDto settings, CancellationToken cancellationToken = default)
    {
        var client = await MyClientAsync(cancellationToken);
        client.ChangeNotificationSettings(
            new NotificationSettings(settings.Appointments, settings.MealReminders, settings.Measurements, settings.Plans));
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<NotificationSettingsDto> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        var settings = (await MyClientAsync(cancellationToken)).Notifications;
        return new NotificationSettingsDto(settings.Appointments, settings.MealReminders, settings.Measurements, settings.Plans);
    }

    public async Task UpdatePushTokenAsync(string? pushToken, CancellationToken cancellationToken = default)
    {
        var token = pushToken?.Trim();
        if (token?.Length > MaxPushTokenLength)
        {
            throw new DomainException($"Push anahtarı en fazla {MaxPushTokenLength} karakter olabilir.");
        }

        var user = await users.GetAsync(currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException("Kullanıcı bulunamadı.");

        user.PushToken = string.IsNullOrEmpty(token) ? null : token;
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Client> MyClientAsync(CancellationToken cancellationToken)
    {
        var clientId = currentUser.ClientId
            ?? throw new InvalidOperationException("Bu işlem yalnızca danışan hesabıyla yapılabilir.");
        var dietitianId = currentUser.DietitianId
            ?? throw new InvalidOperationException("Danışanın bağlı olduğu diyetisyen token'da yok.");

        return await clients.GetAsync(dietitianId, clientId, cancellationToken)
            ?? throw new NotFoundException("Danışan bulunamadı.");
    }
}
