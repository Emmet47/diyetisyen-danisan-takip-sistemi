using Ddts.Application.Abstractions.Persistence;

namespace Ddts.Infrastructure.Persistence;

/// <summary>
/// IUnitOfWork portunun EF Core adapter'ı: bir use case boyunca yapılan değişiklikleri tek seferde kaydeder.
/// ADR-0005'teki domain event dağıtımı (commit sonrası) da ileride buraya eklenecek.
/// </summary>
public sealed class EfUnitOfWork(AppDbContext db) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);
}
