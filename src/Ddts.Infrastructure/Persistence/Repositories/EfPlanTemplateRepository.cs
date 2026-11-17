using Ddts.Application.Abstractions.Persistence;
using Ddts.Domain.Plans;
using Microsoft.EntityFrameworkCore;

namespace Ddts.Infrastructure.Persistence.Repositories;

/// <summary>IPlanTemplateRepository'nin EF Core uygulaması. Şablon öğünleri, satırları ve besinleriyle yüklenir.</summary>
public sealed class EfPlanTemplateRepository(DdtsDbContext db) : IPlanTemplateRepository
{
    public Task<PlanTemplate?> GetAsync(Guid dietitianId, Guid templateId, CancellationToken cancellationToken = default) =>
        db.PlanTemplates
            .Include(t => t.Meals)
            .ThenInclude(m => m.Items)
            .ThenInclude(i => i.FoodItem)
            .AsSplitQuery()
            .SingleOrDefaultAsync(t => t.Id == templateId && t.DietitianId == dietitianId, cancellationToken);

    public async Task<IReadOnlyList<PlanTemplate>> ListAsync(Guid dietitianId, CancellationToken cancellationToken = default) =>
        await db.PlanTemplates
            .AsNoTracking()
            .Where(t => t.DietitianId == dietitianId)
            .OrderBy(t => t.Name)
            .ThenBy(t => t.Id)
            .ToListAsync(cancellationToken);

    public void Add(PlanTemplate template) => db.PlanTemplates.Add(template);

    public void Remove(PlanTemplate template) => db.PlanTemplates.Remove(template);

    public void RemoveMeals(IEnumerable<Meal> meals) => db.Set<Meal>().RemoveRange(meals);
}
