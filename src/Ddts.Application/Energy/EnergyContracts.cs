using Ddts.Domain.Clients;

namespace Ddts.Application.Energy;

/// <summary>Hesapta kullanılan danışan bilgileri: cinsiyet, yaş ve son ölçümdeki kilo/boy.</summary>
public sealed record EnergyInputDto(Sex Sex, int AgeYears, decimal WeightKg, decimal HeightCm);

/// <summary>Enerji hesabı yanıtı (FR-H1, FR-H3, docs/api.md 8.3 örnek 3).</summary>
public sealed record EnergyCalculationDto(
    string Formula,
    EnergyInputDto Input,
    decimal BmrKcal,
    decimal DailyNeedKcal,
    decimal TargetKcal,
    string Disclaimer);
