using Ddts.Application.Abstractions.Events;
using Ddts.Domain.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Ddts.Infrastructure.Events;

/// <summary>
/// Observer deseninde olayları abonelere ileten küçük dağıtıcı (Şekil 4.11, ADR: MediatR ve kuyruk yok).
/// EfUnitOfWork, kayıt (commit) başarılı olduktan sonra çağırır. Bir olay hem kendi türünün hem de üst türünün
/// handler'larına gider (ör. AppointmentRequested → IDomainEventHandler&lt;AppointmentEvent&gt;).
/// Handler hatası loglanır, isteği bozmaz: asıl işlem zaten kaydedildi.
/// </summary>
public sealed class DomainEventDispatcher(IServiceProvider serviceProvider, ILogger<DomainEventDispatcher> logger)
{
    public async Task DispatchAsync(IReadOnlyCollection<IDomainEvent> events, CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in events)
        {
            for (var type = domainEvent.GetType(); type is not null && typeof(IDomainEvent).IsAssignableFrom(type); type = type.BaseType)
            {
                var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(type);
                var handleMethod = handlerType.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.HandleAsync))!;

                foreach (var handler in serviceProvider.GetServices(handlerType))
                {
                    try
                    {
                        await (Task)handleMethod.Invoke(handler, [domainEvent, cancellationToken])!;
                    }
                    catch (Exception exception)
                    {
                        logger.LogError(
                            exception,
                            "{Handler} {Event} olayını işlerken hata verdi.",
                            handler?.GetType().Name,
                            domainEvent.GetType().Name);
                    }
                }
            }
        }
    }
}
