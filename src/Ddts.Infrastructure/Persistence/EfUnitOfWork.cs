using Ddts.Application.Abstractions.Persistence;
using Ddts.Domain.Common;
using Ddts.Infrastructure.Events;

namespace Ddts.Infrastructure.Persistence;

/// <summary>
/// IUnitOfWork'ün EF Core uygulaması. Kaydetmeden önce aggregate'lerin bekleyen domain olaylarını toplar,
/// kayıt başarılı olunca DomainEventDispatcher'a verir (Şekil 4.11). Transaction içindeyse olaylar
/// commit'e kadar bekler: geri alınan bir işlemin bildirimi gitmez (Şekil 5.4).
/// </summary>
public sealed class EfUnitOfWork(DdtsDbContext db, DomainEventDispatcher dispatcher) : IUnitOfWork
{
    private readonly List<IDomainEvent> _pendingEvents = [];
    private bool _inTransaction;

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var events = CollectEvents();
        var result = await db.SaveChangesAsync(cancellationToken);
        _pendingEvents.AddRange(events);

        if (!_inTransaction)
        {
            await DispatchPendingAsync(cancellationToken);
        }

        return result;
    }

    public async Task ExecuteInTransactionAsync(Func<Task> work, CancellationToken cancellationToken = default)
    {
        if (_inTransaction)
        {
            await work();
            return;
        }

        _inTransaction = true;
        try
        {
            // CommitAsync'e ulaşılmadan hata olursa transaction dispose edilirken geri alınır (rollback).
            await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
            await work();
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            _pendingEvents.Clear();
            throw;
        }
        finally
        {
            _inTransaction = false;
        }

        await DispatchPendingAsync(cancellationToken);
    }

    private List<IDomainEvent> CollectEvents()
    {
        var roots = db.ChangeTracker.Entries<AggregateRoot>()
            .Select(entry => entry.Entity)
            .Where(root => root.DomainEvents.Count > 0)
            .ToList();

        var events = roots.SelectMany(root => root.DomainEvents).ToList();
        foreach (var root in roots)
        {
            root.ClearEvents();
        }

        return events;
    }

    private async Task DispatchPendingAsync(CancellationToken cancellationToken)
    {
        if (_pendingEvents.Count == 0)
        {
            return;
        }

        // Handler'lar kendi kayıtlarını yaparken bu listeye yeni olay ekleyebilir: önce kopyalanıp boşaltılır.
        var events = _pendingEvents.ToList();
        _pendingEvents.Clear();
        await dispatcher.DispatchAsync(events, cancellationToken);
    }
}
