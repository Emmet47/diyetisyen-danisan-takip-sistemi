using Ddts.Domain.Foods;

namespace Ddts.Domain.Plans;

/// <summary>
/// Composite deseninin ortak arayüzü (Şekil 4.5). Plan, şablon, öğün ve öğün satırı
/// aynı metotla toplam enerji ve makroyu verir; çağıran taraf hangisiyle çalıştığını bilmek zorunda değil.
/// </summary>
public interface INutritionComponent
{
    NutrientTotals CalculateTotals();
}
