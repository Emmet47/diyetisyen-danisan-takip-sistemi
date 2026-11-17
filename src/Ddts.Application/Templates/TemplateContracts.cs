using Ddts.Application.Plans;
using Ddts.Domain.Plans;

namespace Ddts.Application.Templates;

/// <summary>Şablon oluşturma/düzenleme girdisi (FR-F1). Öğün yapısı plan sihirbazındakiyle aynı.</summary>
public sealed record TemplateCommand(string Name, string? Description, PlanType PlanType, IReadOnlyList<MealInput> Meals);

public sealed record TemplateListItemDto(Guid Id, string Name, string? Description, PlanType PlanType, DateTime UpdatedAt);

/// <summary>Şablonun tamamı ve Composite toplamları.</summary>
public sealed record TemplateSummaryDto(
    Guid Id,
    string Name,
    string? Description,
    PlanType PlanType,
    DateTime UpdatedAt,
    IReadOnlyList<MealDto> Meals,
    IReadOnlyList<DayTotalsDto> Days,
    NutrientTotalsDto Totals,
    string Disclaimer);
