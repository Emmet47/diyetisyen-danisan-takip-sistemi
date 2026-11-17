using Ddts.Domain.Clients;

namespace Ddts.Domain.Energy;

/// <summary>Enerji formüllerinin girdisi (Şekil 4.7): cinsiyet, yaş, son ölçümdeki kilo ve boy.</summary>
public sealed record EnergyInput(Sex Sex, int AgeYears, decimal WeightKg, decimal HeightCm);
