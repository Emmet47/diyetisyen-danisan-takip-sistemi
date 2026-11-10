namespace Ddts.Domain.Common;

/// <summary>
/// Alan içinde gerçekleşen bir olay (Observer / Domain Events deseni).
/// Örnek: PlanPublished, AppointmentConfirmed.
/// </summary>
public interface IDomainEvent
{
    DateTimeOffset OccurredAt { get; }
}
