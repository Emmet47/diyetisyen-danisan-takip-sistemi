namespace Ddts.Domain.Common;

/// <summary>
/// Alan içinde gerçekleşen bir olay (Observer / Domain Events deseni, Şekil 4.11).
/// Örnek: PlanPublished, AppointmentRequested, AppointmentConfirmed.
/// </summary>
public interface IDomainEvent
{
    /// <summary>Olayın gerçekleştiği an (UTC).</summary>
    DateTime OccurredAt { get; }
}
