namespace Ddts.Application.Abstractions.Persistence;

/// <summary>
/// Giden (driven) port: bir use case boyunca yapılan değişiklikleri tek seferde kaydeder.
/// Uygulaması Infrastructure katmanında (EF Core).
/// </summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
