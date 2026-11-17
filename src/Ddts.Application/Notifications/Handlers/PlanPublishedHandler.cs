using Ddts.Application.Abstractions.Events;
using Ddts.Application.Abstractions.Persistence;
using Ddts.Domain.Notifications;
using Ddts.Domain.Plans;

namespace Ddts.Application.Notifications.Handlers;

/// <summary>Observer (Şekil 4.11, 5.4): plan yayımlanınca danışana "Yeni planın hazır" bildirimi (FR-J1).</summary>
public sealed class PlanPublishedHandler(IClientRepository clients, INotificationService notifications)
    : IDomainEventHandler<PlanPublished>
{
    public async Task HandleAsync(PlanPublished domainEvent, CancellationToken cancellationToken = default)
    {
        var client = await clients.GetAsync(domainEvent.DietitianId, domainEvent.ClientId, cancellationToken);
        if (client is null)
        {
            return;
        }

        await notifications.NotifyAsync(
            client.UserId,
            NotificationType.PlanPublished,
            "Yeni planın hazır",
            "Diyetisyenin yeni beslenme planını yayımladı.",
            $"plan:{domainEvent.PlanId}:published",
            cancellationToken);
    }
}
