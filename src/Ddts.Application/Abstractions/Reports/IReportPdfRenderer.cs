using Ddts.Application.Reports;

namespace Ddts.Application.Abstractions.Reports;

/// <summary>İlerleme raporunu PDF'e çeviren port (Şekil 4.4, FR-K2). Adapter: QuestPdfReportRenderer.</summary>
public interface IReportPdfRenderer
{
    byte[] Render(ProgressReportDto report);
}
