namespace Ddts.Domain.Plans;

/// <summary>Plan durumu: Taslak → Yayımlandı → Arşiv (FR-E3, BR-04).</summary>
public enum PlanStatus
{
    Draft,
    Published,
    Archived,
}
