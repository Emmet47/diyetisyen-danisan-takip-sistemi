using Ddts.Application.Abstractions.Persistence;

namespace Ddts.Infrastructure.Persistence;

/// <summary>
/// IUnitOfWork portunun EF Core adapter'ı (Şekil 4.10).
/// ADR-0005'teki domain event dağıtımı (kayıttan sonra DomainEventDispatcher) da ileride buraya eklenecek.
/// </summary>
public sealed class EfUnitOfWork(DdtsDbContext db) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);

    public async Task ExecuteInTransactionAsync(Func<Task> work, CancellationToken cancellationToken = default)
    {
        // CommitAsync'e ulaşılmadan hata olursa transaction dispose edilirken geri alınır (rollback).
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await work();
        await transaction.CommitAsync(cancellationToken);
    }
}
