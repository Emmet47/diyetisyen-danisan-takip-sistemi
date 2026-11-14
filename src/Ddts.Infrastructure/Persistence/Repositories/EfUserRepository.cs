using Ddts.Application.Abstractions.Persistence;
using Ddts.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Ddts.Infrastructure.Persistence.Repositories;

/// <summary>IUserRepository'nin EF Core uygulaması.</summary>
public sealed class EfUserRepository(DdtsDbContext db) : IUserRepository
{
    public Task<User?> GetAsync(Guid userId, CancellationToken cancellationToken = default) =>
        db.Users.SingleOrDefaultAsync(u => u.Id == userId, cancellationToken);

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(email);
        return db.Users.SingleOrDefaultAsync(u => u.Email == normalized, cancellationToken);
    }

    // ResetTokenHash sınıfta private; sorguda adıyla kullanılır.
    public Task<User?> GetByResetTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        db.Users.FirstOrDefaultAsync(u => EF.Property<string>(u, "ResetTokenHash") == tokenHash, cancellationToken);

    public Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalized = Normalize(email);
        return db.Users.AnyAsync(u => u.Email == normalized, cancellationToken);
    }

    public void Add(User user) => db.Users.Add(user);

    public void Remove(User user) => db.Users.Remove(user);

    // User sınıfı e-postayı aynı şekilde saklar (küçük harf, boşluksuz).
    private static string Normalize(string email) => email.Trim().ToLowerInvariant();
}
