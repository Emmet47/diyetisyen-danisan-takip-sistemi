using Ddts.Application.Abstractions.Persistence;
using Ddts.Domain.Clients;
using Microsoft.EntityFrameworkCore;

namespace Ddts.Infrastructure.Persistence.Repositories;

/// <summary>
/// IClientRepository'nin EF Core uygulaması (Şekil 4.10'daki EfClientRepository).
/// Her sorgu dietitianId ile süzülür: başka diyetisyenin danışanı hiç dönmez (Şekil 5.9).
/// </summary>
public sealed class EfClientRepository(DdtsDbContext db) : IClientRepository
{
    public Task<Client?> GetAsync(Guid dietitianId, Guid clientId, CancellationToken cancellationToken = default) =>
        db.Clients
            .Include(c => c.Restrictions)
            .SingleOrDefaultAsync(c => c.Id == clientId && c.DietitianId == dietitianId, cancellationToken);

    public async Task<IReadOnlyList<Client>> ListAsync(Guid dietitianId, bool onlyActive, CancellationToken cancellationToken = default)
    {
        var query = db.Clients.AsNoTracking().Where(c => c.DietitianId == dietitianId);

        if (onlyActive)
        {
            query = query.Where(c => c.IsActive);
        }

        return await query.OrderBy(c => c.FullName).ToListAsync(cancellationToken);
    }

    public Task<int> CountActiveAsync(Guid dietitianId, CancellationToken cancellationToken = default) =>
        db.Clients.CountAsync(c => c.DietitianId == dietitianId && c.IsActive, cancellationToken);

    public void Add(Client client) => db.Clients.Add(client);

    public void Remove(Client client) => db.Clients.Remove(client);
}
