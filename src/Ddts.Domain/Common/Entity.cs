namespace Ddts.Domain.Common;

/// <summary>
/// Tüm varlıkların (entity) ortak tabanı: kimlik (Id) ve henüz dağıtılmamış domain event'ler.
/// </summary>
public abstract class Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// Zamana göre sıralı GUID (v7). Rastgele GUID'e göre PostgreSQL indeksinde daha düzenli durur.
    /// </summary>
    public Guid Id { get; protected init; } = Guid.CreateVersion7();

    /// <summary>Varlık içinde önemli bir olay olduğunda çağrılır (ör. plan yayımlandı).</summary>
    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    /// <summary>
    /// Biriken event'leri döndürür ve listeyi boşaltır.
    /// Commit sonrası çalışan dispatcher bu metodu kullanır.
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> PullDomainEvents()
    {
        var events = _domainEvents.ToArray();
        _domainEvents.Clear();
        return events;
    }
}
