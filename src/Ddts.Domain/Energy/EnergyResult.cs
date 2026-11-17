namespace Ddts.Domain.Energy;

/// <summary>
/// Enerji hesabının sonucu (FR-H1, FR-H3). Kcal değerleri 2 basamağa yuvarlanır.
/// Disclaimer her sonuçta gösterilir: hesap tahminidir, profesyonel değerlendirme yerine geçmez.
/// </summary>
public sealed record EnergyResult(string Formula, decimal BmrKcal, decimal DailyNeedKcal, decimal TargetKcal, string Disclaimer)
{
    public const string DefaultDisclaimer = "Hesaplamalar tahminidir, profesyonel değerlendirme yerine geçmez.";
}
