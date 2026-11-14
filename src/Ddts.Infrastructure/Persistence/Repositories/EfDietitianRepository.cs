using Ddts.Application.Abstractions.Persistence;
using Ddts.Domain.Dietitians;
using Microsoft.EntityFrameworkCore;

namespace Ddts.Infrastructure.Persistence.Repositories;

/// <summary>IDietitianRepository'nin EF Core uygulaması. Çalışma saatleri de birlikte yüklenir.</summary>
public sealed class EfDietitianRepository(DdtsDbContext db) : IDietitianRepository
{
    public Task<Dietitian?> GetAsync(Guid dietitianId, CancellationToken cancellationToken = default) =>
        db.Dietitians
            .Include(d => d.WorkingHours)
            .SingleOrDefaultAsync(d => d.Id == dietitianId, cancellationToken);

    public async Task<IReadOnlyList<Dietitian>> ListAsync(int page, int pageSize, CancellationToken cancellationToken = default) =>
        await db.Dietitians
            .AsNoTracking()
            .OrderBy(d => d.FullName)
            .ThenBy(d => d.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

    public Task<int> CountAsync(CancellationToken cancellationToken = default) =>
        db.Dietitians.CountAsync(cancellationToken);

    public void Add(Dietitian dietitian) => db.Dietitians.Add(dietitian);
}
