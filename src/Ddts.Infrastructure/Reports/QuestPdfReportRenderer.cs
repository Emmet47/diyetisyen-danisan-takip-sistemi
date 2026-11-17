using System.Globalization;
using Ddts.Application.Abstractions.Reports;
using Ddts.Application.Common;
using Ddts.Application.Reports;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Ddts.Infrastructure.Reports;

/// <summary>
/// IReportPdfRenderer'ın QuestPDF adapter'ı (Şekil 4.4, FR-K2). Dönemsel ilerleme raporunu A4 PDF olarak üretir:
/// özet (kilo/bel değişimi, hedefe kalan kilo, plan uyumu, kayıt sıklığı) ve dönemin ölçüm tablosu.
/// Grafikler web panelinde çizilir. QuestPDF topluluk lisansıyla kullanılır (yıllık geliri 1 milyon $ altı).
/// </summary>
public sealed class QuestPdfReportRenderer : IReportPdfRenderer
{
    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");

    static QuestPdfReportRenderer()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        // QuestPDF'in içindeki Lato yazı tipi Türkçe karakterleri içerir; eksik karakterde hata yerine boşluk basılır.
        QuestPDF.Settings.CheckIfAllTextGlyphsAreAvailable = false;
    }

    public byte[] Render(ProgressReportDto report) => Document.Create(document =>
    {
        document.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(2, Unit.Centimetre);
            page.DefaultTextStyle(style => style.FontSize(10));

            page.Header().Column(header =>
            {
                header.Item().Text("İlerleme Raporu").FontSize(18).Bold();
                header.Item().Text($"{report.ClientName} · {Date(report.From)} – {Date(report.To)}");
            });

            page.Content().PaddingVertical(12).Column(content =>
            {
                content.Spacing(10);

                content.Item().Text("Özet").FontSize(13).Bold();
                content.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(2);
                        columns.RelativeColumn(1);
                    });

                    Row(table, "Kilo değişimi (dönem)", Signed(report.WeightChangeKg, "kg"));
                    Row(table, "Bel çevresi değişimi (dönem)", Signed(report.WaistChangeCm, "cm"));
                    Row(table, "Hedef kilo", Number(report.TargetWeightKg, "kg"));
                    Row(table, "Hedefe kalan kilo", Number(report.RemainingKg, "kg"));
                    Row(table, "Plan uyumu", report.AdherencePercent is { } adherence
                        ? $"%{Number(adherence)} ({report.EatenMeals} yedim, {report.PartialMeals} kısmen, {report.PlannedMeals} planlı öğün)"
                        : "Dönemde planlı öğün yok");
                    Row(table, "Öğün kayıt sıklığı", $"%{Number(report.LogFrequencyPercent)} ({report.LoggedDays}/{report.DayCount} gün)");
                });

                content.Item().Text("Ölçümler").FontSize(13).Bold();

                if (report.Measurements.Count == 0)
                {
                    content.Item().Text("Bu dönemde ölçüm yok.");
                }
                else
                {
                    content.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                        });

                        table.Header(cells =>
                        {
                            foreach (var title in new[] { "Tarih", "Kilo (kg)", "Fark (kg)", "Bel (cm)", "Fark (cm)" })
                            {
                                cells.Cell().BorderBottom(1).PaddingBottom(4).Text(title).Bold();
                            }
                        });

                        foreach (var m in report.Measurements)
                        {
                            table.Cell().PaddingVertical(2).Text(Date(m.MeasuredOn));
                            table.Cell().PaddingVertical(2).Text(Number(m.WeightKg));
                            table.Cell().PaddingVertical(2).Text(Signed(m.WeightChangeKg, string.Empty));
                            table.Cell().PaddingVertical(2).Text(Number(m.WaistCm));
                            table.Cell().PaddingVertical(2).Text(Signed(m.WaistChangeCm, string.Empty));
                        }
                    });
                }

                content.Item().PaddingTop(8).Text(report.Disclaimer).Italic().FontSize(9);
            });

            page.Footer().AlignCenter().Text(text =>
            {
                text.Span($"Oluşturulma: {LocalTime.Format(report.GeneratedAt)} · Sayfa ");
                text.CurrentPageNumber();
                text.Span(" / ");
                text.TotalPages();
            });
        });
    }).GeneratePdf();

    private static void Row(TableDescriptor table, string label, string value)
    {
        table.Cell().PaddingVertical(3).Text(label);
        table.Cell().PaddingVertical(3).Text(value).Bold();
    }

    private static string Date(DateOnly date) => date.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);

    private static string Number(decimal value) => value.ToString("0.0", Turkish);

    private static string Number(decimal? value, string unit) => value is { } v ? $"{Number(v)} {unit}" : "—";

    private static string Signed(decimal? value, string unit) =>
        value is { } v ? $"{(v > 0 ? "+" : string.Empty)}{Number(v)} {unit}".TrimEnd() : "—";
}
