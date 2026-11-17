using Ddts.Domain.Common;

namespace Ddts.Application.Abstractions.Events;

/// <summary>
/// Observer deseninin gözlemci arayüzü (Şekil 4.11). Bir domain olayı kaydedildikten (commit) sonra
/// DomainEventDispatcher, olayın türüne (ve üst türlerine) abone olan bütün handler'ları çağırır.
/// </summary>
public interface IDomainEventHandler<in TEvent>
    where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}
