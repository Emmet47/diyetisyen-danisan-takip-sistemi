namespace Ddts.Domain.Foods;

/// <summary>
/// Enerji ve makro toplamları (değer nesnesi, Şekil 4.3). Composite deseninde her bileşenin
/// CalculateTotals() sonucu bu tiptedir; toplamlar Add ile birleştirilir (Şekil 4.5).
/// record olduğu için iki toplam, değerleri aynıysa eşittir.
/// </summary>
public sealed record NutrientTotals(decimal EnergyKcal, decimal ProteinG, decimal CarbsG, decimal FatG)
{
    // Makro yüzdeleri için Atwater katsayıları: 1 g protein ve karbonhidrat 4 kcal, 1 g yağ 9 kcal.
    private const decimal KcalPerGramProtein = 4m;
    private const decimal KcalPerGramCarbs = 4m;
    private const decimal KcalPerGramFat = 9m;

    /// <summary>Boş toplam (toplama işleminin başlangıç değeri).</summary>
    public static NutrientTotals Zero { get; } = new(0m, 0m, 0m, 0m);

    /// <summary>Makrolardan gelen enerjinin yüzde kaçı proteinden (FR-H2: makro dağılımı).</summary>
    public decimal ProteinPercent => PercentOfMacroEnergy(ProteinG * KcalPerGramProtein);

    public decimal CarbsPercent => PercentOfMacroEnergy(CarbsG * KcalPerGramCarbs);

    public decimal FatPercent => PercentOfMacroEnergy(FatG * KcalPerGramFat);

    public NutrientTotals Add(NutrientTotals other) =>
        new(EnergyKcal + other.EnergyKcal, ProteinG + other.ProteinG, CarbsG + other.CarbsG, FatG + other.FatG);

    // Yüzdeler makro enerjisinin toplamına göre: üçünün toplamı (yuvarlama dışında) 100 eder.
    private decimal PercentOfMacroEnergy(decimal kcal)
    {
        var macroKcal = ProteinG * KcalPerGramProtein + CarbsG * KcalPerGramCarbs + FatG * KcalPerGramFat;
        return macroKcal == 0m ? 0m : Math.Round(kcal * 100m / macroKcal, 1);
    }
}
