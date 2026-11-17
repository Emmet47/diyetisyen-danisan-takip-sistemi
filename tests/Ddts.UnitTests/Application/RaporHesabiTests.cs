using Ddts.Application.Reports;

namespace Ddts.UnitTests.Application;

/// <summary>Rapor hesapları (FR-K3, UT-13).</summary>
public sealed class RaporHesabiTests
{
    [Fact]
    public void Plan_uyumu_yedim_arti_yarim_kismen_bolu_planli_ogun()
    {
        // 14 planlı öğün, 8 Yedim + 2 Kısmen → (8 + 1) / 14 = %64,3 (UT-13)
        Assert.Equal(64.3m, ReportService.AdherencePercent(eaten: 8, partial: 2, planned: 14));
        Assert.Equal(100m, ReportService.AdherencePercent(eaten: 3, partial: 0, planned: 3));
        Assert.Null(ReportService.AdherencePercent(eaten: 0, partial: 0, planned: 0));
    }
}
