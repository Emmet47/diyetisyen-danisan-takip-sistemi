namespace Ddts.Application.Reports;

/// <summary>Gelen port: ilerleme paneli ve PDF raporu (K modülü).</summary>
public interface IReportService
{
    /// <summary>Panel verisi (/api/clients/{id}/progress). Varsayılan dönem: son 30 gün; en fazla 366 gün.</summary>
    Task<ProgressReportDto> GetProgressAsync(Guid clientId, DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default);

    /// <summary>Aynı raporun PDF'i (/api/clients/{id}/report.pdf).</summary>
    Task<ReportFileDto> RenderPdfAsync(Guid clientId, DateOnly? from, DateOnly? to, CancellationToken cancellationToken = default);
}
