using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using EconomIA.Application.DTOs;
using EconomIA.Application.Interfaces;
using EconomIA.Infrastructure.Persistence;
using EconomIA.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EconomIA.Infrastructure.ExternalServices;

public class ExportService : IExportService
{
    private readonly EconomIADbContext _db;

    public ExportService(EconomIADbContext db)
    {
        _db = db;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public async Task<ExportResult> ExportFundsAsync(string format, Dictionary<string, string>? filters = null)
    {
        var funds = await _db.Set<Fund>()
            .AsNoTracking()
            .Include(f => f.Performances)
            .OrderBy(f => f.RankingPosition)
            .Take(100)
            .ToListAsync();

        if (format == "excel")
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Fondos");
            ws.Cell(1, 1).Value = "Nombre";
            ws.Cell(1, 2).Value = "ISIN";
            ws.Cell(1, 3).Value = "Categoría";
            ws.Cell(1, 4).Value = "Rent. 1A (%)";
            ws.Cell(1, 5).Value = "Rating";
            ws.Cell(1, 6).Value = "TER (%)";
            ws.Cell(1, 7).Value = "NAV";
            ws.Cell(1, 8).Value = "Riesgo";
            ws.Row(1).Style.Font.Bold = true;

            for (int i = 0; i < funds.Count; i++)
            {
                var f = funds[i];
                var perf = f.Performances.OrderByDescending(p => p.RecordedAt).FirstOrDefault();
                ws.Cell(i + 2, 1).Value = f.Name;
                ws.Cell(i + 2, 2).Value = f.Isin.ToString();
                ws.Cell(i + 2, 3).Value = f.Category;
                ws.Cell(i + 2, 4).Value = (double)(perf?.Return1Year.Value ?? 0);
                ws.Cell(i + 2, 5).Value = (int)f.Rating;
                ws.Cell(i + 2, 6).Value = (double)f.ExpenseRatio.Value;
                ws.Cell(i + 2, 7).Value = (double)f.NetAssetValue.Amount;
                ws.Cell(i + 2, 8).Value = (int)f.RiskLevel;
            }

            ws.Columns().AdjustToContents();
            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return new ExportResult(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"fondos_{DateTime.Now:yyyyMMdd}.xlsx");
        }

        // PDF
        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(30);
                page.Header().Text("economIA — Ranking de Fondos").FontSize(18).Bold().FontColor(Colors.Blue.Darken2);
                page.Content().PaddingVertical(10).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(3);
                        c.RelativeColumn(2);
                        c.RelativeColumn(2);
                        c.RelativeColumn(1);
                        c.RelativeColumn(1);
                        c.RelativeColumn(1);
                    });

                    table.Header(header =>
                    {
                        header.Cell().Text("Nombre").Bold();
                        header.Cell().Text("ISIN").Bold();
                        header.Cell().Text("Categoría").Bold();
                        header.Cell().Text("Rent. 1A").Bold();
                        header.Cell().Text("Rating").Bold();
                        header.Cell().Text("TER").Bold();
                    });

                    foreach (var f in funds)
                    {
                        var perf = f.Performances.OrderByDescending(p => p.RecordedAt).FirstOrDefault();
                        table.Cell().Text(f.Name).FontSize(9);
                        table.Cell().Text(f.Isin.ToString()).FontSize(9);
                        table.Cell().Text(f.Category).FontSize(9);
                        table.Cell().Text($"{perf?.Return1Year.Value ?? 0:F2}%").FontSize(9);
                        table.Cell().Text($"{(int)f.Rating}").FontSize(9);
                        table.Cell().Text($"{f.ExpenseRatio.Value:F2}%").FontSize(9);
                    }
                });
                page.Footer().AlignCenter().Text(t =>
                {
                    t.Span("Generado el ").FontSize(8);
                    t.Span(DateTime.Now.ToString("dd/MM/yyyy HH:mm")).FontSize(8).Bold();
                    t.Span(" · economIA v2").FontSize(8);
                });
            });
        });

        var pdfBytes = pdf.GeneratePdf();
        return new ExportResult(pdfBytes, "application/pdf", $"fondos_{DateTime.Now:yyyyMMdd}.pdf");
    }

    public async Task<ExportResult> ExportPortfolioAsync(string format, string watchlistId)
    {
        var watchlist = await _db.Set<Watchlist>()
            .AsNoTracking()
            .Include(w => w.Items)
            .FirstOrDefaultAsync(w => w.Id == Guid.Parse(watchlistId));

        var name = watchlist?.Name ?? "Cartera";
        var items = watchlist?.Items.ToList() ?? [];

        if (format == "excel")
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add(name);
            ws.Cell(1, 1).Value = "Tipo";
            ws.Cell(1, 2).Value = "Posición";
            ws.Cell(1, 3).Value = "Notas";
            ws.Cell(1, 4).Value = "Prioridad";
            ws.Cell(1, 5).Value = "Fecha";
            ws.Row(1).Style.Font.Bold = true;

            for (int i = 0; i < items.Count; i++)
            {
                ws.Cell(i + 2, 1).Value = items[i].EntityType;
                ws.Cell(i + 2, 2).Value = items[i].PositionType;
                ws.Cell(i + 2, 3).Value = items[i].Notes ?? "";
                ws.Cell(i + 2, 4).Value = items[i].Priority;
                ws.Cell(i + 2, 5).Value = items[i].CreatedAt.ToString("dd/MM/yyyy");
            }
            ws.Columns().AdjustToContents();
            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return new ExportResult(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"{name}_{DateTime.Now:yyyyMMdd}.xlsx");
        }

        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.Header().Text($"economIA — {name}").FontSize(16).Bold();
                page.Content().PaddingVertical(10).Table(table =>
                {
                    table.ColumnsDefinition(c => { c.RelativeColumn(1); c.RelativeColumn(1); c.RelativeColumn(3); c.RelativeColumn(1); });
                    table.Header(h => { h.Cell().Text("Tipo").Bold(); h.Cell().Text("Posición").Bold(); h.Cell().Text("Notas").Bold(); h.Cell().Text("Fecha").Bold(); });
                    foreach (var item in items)
                    {
                        table.Cell().Text(item.EntityType).FontSize(10);
                        table.Cell().Text(item.PositionType).FontSize(10);
                        table.Cell().Text(item.Notes ?? "").FontSize(10);
                        table.Cell().Text(item.CreatedAt.ToString("dd/MM/yyyy")).FontSize(10);
                    }
                });
                page.Footer().AlignCenter().Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8);
            });
        });
        return new ExportResult(pdf.GeneratePdf(), "application/pdf", $"{name}_{DateTime.Now:yyyyMMdd}.pdf");
    }

    public async Task<ExportResult> ExportEarningsCallAsync(string format, string callId)
    {
        var call = await _db.Set<EarningsCall>()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == Guid.Parse(callId));

        var title = call != null ? $"{call.CompanyName} Q{call.FiscalQuarter} {call.FiscalYear}" : "Llamada";

        if (format == "excel")
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Análisis");
            ws.Cell(1, 1).Value = "Campo"; ws.Cell(1, 2).Value = "Valor"; ws.Row(1).Style.Font.Bold = true;
            ws.Cell(2, 1).Value = "Empresa"; ws.Cell(2, 2).Value = call?.CompanyName ?? "";
            ws.Cell(3, 1).Value = "Trimestre"; ws.Cell(3, 2).Value = $"Q{call?.FiscalQuarter} {call?.FiscalYear}";
            ws.Cell(4, 1).Value = "Sentimiento"; ws.Cell(4, 2).Value = call?.Sentiment ?? "";
            ws.Cell(5, 1).Value = "Resumen"; ws.Cell(5, 2).Value = call?.Summary ?? "";
            ws.Cell(6, 1).Value = "Previsiones"; ws.Cell(6, 2).Value = call?.Guidance ?? "";
            ws.Cell(7, 1).Value = "Métricas clave"; ws.Cell(7, 2).Value = call?.KeyMetrics ?? "";
            ws.Columns().AdjustToContents();
            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return new ExportResult(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"earnings_{title.Replace(' ', '_')}_{DateTime.Now:yyyyMMdd}.xlsx");
        }

        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.Header().Text($"economIA — Llamada de Resultados: {title}").FontSize(14).Bold();
                page.Content().PaddingVertical(10).Column(col =>
                {
                    if (call?.Summary != null)
                    {
                        col.Item().Text("Resumen").Bold().FontSize(12);
                        col.Item().PaddingBottom(5).Text(call.Summary).FontSize(10);
                    }
                    if (call?.Guidance != null)
                    {
                        col.Item().Text("Previsiones").Bold().FontSize(12);
                        col.Item().PaddingBottom(5).Text(call.Guidance).FontSize(10);
                    }
                    if (call?.KeyMetrics != null)
                    {
                        col.Item().Text("Métricas Clave").Bold().FontSize(12);
                        col.Item().PaddingBottom(5).Text(call.KeyMetrics).FontSize(10);
                    }
                    col.Item().PaddingTop(10).Text($"Sentimiento: {call?.Sentiment ?? "N/A"}").FontSize(10).Italic();
                });
                page.Footer().AlignCenter().Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8);
            });
        });
        return new ExportResult(pdf.GeneratePdf(), "application/pdf", $"earnings_{title.Replace(' ', '_')}_{DateTime.Now:yyyyMMdd}.pdf");
    }

    public async Task<ExportResult> ExportReportAsync(string format, string reportId)
    {
        var report = await _db.Set<AIReport>()
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == Guid.Parse(reportId));

        var title = report?.Title ?? "Informe";

        if (format == "excel")
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("Informe");
            ws.Cell(1, 1).Value = "Título"; ws.Cell(1, 2).Value = title; ws.Row(1).Style.Font.Bold = true;
            ws.Cell(2, 1).Value = "Tipo"; ws.Cell(2, 2).Value = report?.ReportType ?? "";
            ws.Cell(3, 1).Value = "Contenido"; ws.Cell(3, 2).Value = report?.Content ?? "";
            ws.Cell(4, 1).Value = "Generado"; ws.Cell(4, 2).Value = report?.CreatedAt.ToString("dd/MM/yyyy HH:mm") ?? "";
            ws.Columns().AdjustToContents();
            using var ms = new MemoryStream();
            wb.SaveAs(ms);
            return new ExportResult(ms.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"informe_{DateTime.Now:yyyyMMdd}.xlsx");
        }

        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.Header().Text($"economIA — {title}").FontSize(14).Bold();
                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Item().Text($"Tipo: {report?.ReportType}").FontSize(10).Italic();
                    col.Item().PaddingTop(10).Text(report?.Content ?? "Sin contenido").FontSize(10);
                });
                page.Footer().AlignCenter().Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8);
            });
        });
        return new ExportResult(pdf.GeneratePdf(), "application/pdf", $"informe_{DateTime.Now:yyyyMMdd}.pdf");
    }
}
