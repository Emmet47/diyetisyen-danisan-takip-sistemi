namespace Ddts.Domain.Common;

/// <summary>
/// Domain event üreten varlıkların ortak tabanı: Observer deseninde "Subject" (Şekil 4.11).
/// Alan modelinde NutritionPlan ve Appointment bu sınıftan türer (Şekil 4.1, 4.2).
/// Kimlik (Id) her varlığın kendi sınıfında tanımlıdır.
/// </summary>
public abstract class AggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// Henüz dağıtılmamış (bekleyen) olaylar. EfUnitOfWork kayıttan (commit) sonra
    /// bunları DomainEventDispatcher'a verir, sonra ClearEvents ile listeyi boşaltır (ADR-0005).
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>Varlık içinde önemli bir olay olduğunda çağrılır (ör. PlanPublished, AppointmentConfirmed).</summary>
    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    /// <summary>Bekleyen olayları siler.</summary>
    public void ClearEvents() => _domainEvents.Clear();
}
