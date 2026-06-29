using System.Text;
using System.Text.RegularExpressions;
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
    private static readonly string BrandColor = Colors.Blue.Darken2;

    public ExportService(EconomIADbContext db)
    {
        _db = db;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    // ── Markdown → QuestPDF rendering ──────────────────────────────────

    private static void RenderMarkdownToPdf(ColumnDescriptor col, string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            col.Item().Text("Sin contenido").FontSize(10).Italic();
            return;
        }

        var lines = markdown.Split('\n');
        foreach (var rawLine in lines)
        {
            var line = rawLine.TrimEnd('\r');

            // Blank line → spacing
            if (string.IsNullOrWhiteSpace(line))
            {
                col.Item().PaddingBottom(4);
                continue;
            }

            // Headers
            if (line.StartsWith("### "))
            {
                col.Item().PaddingTop(6).PaddingBottom(2)
                    .Text(StripInlineMarkdown(line[4..])).FontSize(11).Bold().FontColor(Colors.Grey.Darken3);
                continue;
            }
            if (line.StartsWith("## "))
            {
                col.Item().PaddingTop(8).PaddingBottom(3)
                    .Text(StripInlineMarkdown(line[3..])).FontSize(13).Bold().FontColor(BrandColor);
                continue;
            }
            if (line.StartsWith("# "))
            {
                col.Item().PaddingTop(10).PaddingBottom(4)
                    .Text(StripInlineMarkdown(line[2..])).FontSize(15).Bold().FontColor(BrandColor);
                continue;
            }

            // Horizontal rule
            if (Regex.IsMatch(line.Trim(), @"^[-*_]{3,}$"))
            {
                col.Item().PaddingVertical(4).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
                continue;
            }

            // Bullet list
            if (line.TrimStart().StartsWith("- ") || line.TrimStart().StartsWith("* "))
            {
                var indent = line.Length - line.TrimStart().Length;
                var text = StripInlineMarkdown(line.TrimStart()[2..]);
                col.Item().PaddingLeft(10 + indent * 8).Row(row =>
                {
                    row.ConstantItem(10).AlignMiddle().Text("•").FontSize(9).Bold().FontColor(BrandColor);
                    row.RelativeItem().Text(text).FontSize(10);
                });
                continue;
            }

            // Numbered list
            var numberedMatch = Regex.Match(line.TrimStart(), @"^(\d+)\.\s+(.+)$");
            if (numberedMatch.Success)
            {
                var num = numberedMatch.Groups[1].Value;
                var text = StripInlineMarkdown(numberedMatch.Groups[2].Value);
                col.Item().PaddingLeft(10).Row(row =>
                {
                    row.ConstantItem(18).AlignMiddle().Text($"{num}.").FontSize(10).Bold().FontColor(BrandColor);
                    row.RelativeItem().Text(text).FontSize(10);
                });
                continue;
            }

            // Regular paragraph — render inline bold/italic
            col.Item().Text(t =>
            {
                t.DefaultTextStyle(x => x.FontSize(10));
                RenderInlineMarkdown(t, line);
            });
        }
    }

    private static void RenderInlineMarkdown(TextDescriptor text, string line)
    {
        // Split on **bold** and *italic* patterns
        var parts = Regex.Split(line, @"(\*\*[^*]+\*\*|\*[^*]+\*)");
        foreach (var part in parts)
        {
            if (part.StartsWith("**") && part.EndsWith("**"))
                text.Span(part[2..^2]).Bold();
            else if (part.StartsWith("*") && part.EndsWith("*"))
                text.Span(part[1..^1]).Italic();
            else
                text.Span(part);
        }
    }

    private static string StripInlineMarkdown(string text)
    {
        text = Regex.Replace(text, @"\*\*([^*]+)\*\*", "$1");
        text = Regex.Replace(text, @"\*([^*]+)\*", "$1");
        text = Regex.Replace(text, @"`([^`]+)`", "$1");
        return text.Trim();
    }

    private static void AddPdfHeader(PageDescriptor page, string title)
    {
        page.Header().BorderBottom(1).BorderColor(BrandColor).PaddingBottom(5).Row(row =>
        {
            row.RelativeItem().Text(title).FontSize(16).Bold().FontColor(BrandColor);
            row.ConstantItem(120).AlignRight().AlignBottom()
                .Text(DateTime.Now.ToString("dd/MM/yyyy")).FontSize(9).FontColor(Colors.Grey.Medium);
        });
    }

    private static void AddPdfFooter(PageDescriptor page)
    {
        page.Footer().BorderTop(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingTop(4).Row(row =>
        {
            row.RelativeItem().Text("economIA · Herramienta de apoyo al análisis financiero").FontSize(7).FontColor(Colors.Grey.Medium);
            row.ConstantItem(80).AlignRight().Text(t =>
            {
                t.Span("Pág. ").FontSize(7).FontColor(Colors.Grey.Medium);
                t.CurrentPageNumber().FontSize(7).FontColor(Colors.Grey.Medium);
            });
        });
    }

    public async Task<ExportResult> ExportFundsAsync(string format, Dictionary<string, string>? filters = null)
    {
        var funds = await _db.Set<Fund>()
            .AsNoTracking()
            .Include(f => f.Performances)
            .OrderBy(f => f.RankingPosition)
            .Take(100)
            .ToListAsync();

        if (format == "md")
        {
            var sb = new StringBuilder();
            sb.AppendLine("# economIA — Ranking de Fondos");
            sb.AppendLine($"*Generado: {DateTime.Now:dd/MM/yyyy HH:mm}*\n");
            sb.AppendLine("| # | Nombre | ISIN | Categoría | Rent. 1A | Rating | TER |");
            sb.AppendLine("|---|--------|------|-----------|----------|--------|-----|");
            for (int i = 0; i < funds.Count; i++)
            {
                var f = funds[i];
                var perf = f.Performances.OrderByDescending(p => p.RecordedAt).FirstOrDefault();
                sb.AppendLine($"| {i + 1} | {f.Name} | {f.Isin} | {f.Category} | {perf?.Return1Year.Value ?? 0:F2}% | {(int)f.Rating}★ | {f.ExpenseRatio.Value:F2}% |");
            }
            return new ExportResult(Encoding.UTF8.GetBytes(sb.ToString()), "text/markdown", $"fondos_{DateTime.Now:yyyyMMdd}.md");
        }

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
                AddPdfHeader(page, "Ranking de Fondos");
                page.Content().PaddingVertical(10).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.ConstantColumn(25);
                        c.RelativeColumn(3);
                        c.RelativeColumn(2);
                        c.RelativeColumn(2);
                        c.RelativeColumn(1);
                        c.RelativeColumn(1);
                        c.RelativeColumn(1);
                    });

                    table.Header(header =>
                    {
                        void HeaderCell(IContainer c, string text) =>
                            c.Background(BrandColor).Padding(4).Text(text).FontSize(9).Bold().FontColor(Colors.White);
                        HeaderCell(header.Cell(), "#");
                        HeaderCell(header.Cell(), "Nombre");
                        HeaderCell(header.Cell(), "ISIN");
                        HeaderCell(header.Cell(), "Categoría");
                        HeaderCell(header.Cell(), "Rent. 1A");
                        HeaderCell(header.Cell(), "Rating");
                        HeaderCell(header.Cell(), "TER");
                    });

                    for (int i = 0; i < funds.Count; i++)
                    {
                        var f = funds[i];
                        var perf = f.Performances.OrderByDescending(p => p.RecordedAt).FirstOrDefault();
                        var bg = i % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;
                        void DataCell(IContainer c, string text) =>
                            c.Background(bg).Padding(3).Text(text).FontSize(9);
                        DataCell(table.Cell(), $"{i + 1}");
                        DataCell(table.Cell(), f.Name);
                        DataCell(table.Cell(), f.Isin.ToString());
                        DataCell(table.Cell(), f.Category);
                        DataCell(table.Cell(), $"{perf?.Return1Year.Value ?? 0:F2}%");
                        DataCell(table.Cell(), $"{"★".PadLeft((int)f.Rating, '★')}");
                        DataCell(table.Cell(), $"{f.ExpenseRatio.Value:F2}%");
                    }
                });
                AddPdfFooter(page);
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

        if (format == "md")
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# economIA — {name}");
            sb.AppendLine($"*Generado: {DateTime.Now:dd/MM/yyyy HH:mm}*\n");
            sb.AppendLine("| Tipo | Posición | Notas | Prioridad | Fecha |");
            sb.AppendLine("|------|----------|-------|-----------|-------|");
            foreach (var item in items)
                sb.AppendLine($"| {item.EntityType} | {item.PositionType} | {item.Notes ?? "-"} | {item.Priority} | {item.CreatedAt:dd/MM/yyyy} |");
            return new ExportResult(Encoding.UTF8.GetBytes(sb.ToString()), "text/markdown", $"{name}_{DateTime.Now:yyyyMMdd}.md");
        }

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
                AddPdfHeader(page, name);
                page.Content().PaddingVertical(10).Table(table =>
                {
                    table.ColumnsDefinition(c => { c.RelativeColumn(1); c.RelativeColumn(1); c.RelativeColumn(3); c.RelativeColumn(1); });
                    table.Header(h =>
                    {
                        void H(IContainer c, string t) => c.Background(BrandColor).Padding(4).Text(t).FontSize(9).Bold().FontColor(Colors.White);
                        H(h.Cell(), "Tipo"); H(h.Cell(), "Posición"); H(h.Cell(), "Notas"); H(h.Cell(), "Fecha");
                    });
                    for (int i = 0; i < items.Count; i++)
                    {
                        var item = items[i];
                        var bg = i % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;
                        void D(IContainer c, string t) => c.Background(bg).Padding(3).Text(t).FontSize(10);
                        D(table.Cell(), item.EntityType);
                        D(table.Cell(), item.PositionType);
                        D(table.Cell(), item.Notes ?? "");
                        D(table.Cell(), item.CreatedAt.ToString("dd/MM/yyyy"));
                    }
                });
                AddPdfFooter(page);
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

        if (format == "md")
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# Llamada de Resultados: {title}");
            sb.AppendLine($"*Generado: {DateTime.Now:dd/MM/yyyy HH:mm}*\n");
            sb.AppendLine($"**Sentimiento:** {call?.Sentiment ?? "N/A"}\n");
            if (call?.Summary != null) { sb.AppendLine("## Resumen\n"); sb.AppendLine(call.Summary + "\n"); }
            if (call?.Guidance != null) { sb.AppendLine("## Previsiones\n"); sb.AppendLine(call.Guidance + "\n"); }
            if (call?.KeyMetrics != null) { sb.AppendLine("## Métricas Clave\n"); sb.AppendLine(call.KeyMetrics + "\n"); }
            return new ExportResult(Encoding.UTF8.GetBytes(sb.ToString()), "text/markdown", $"earnings_{title.Replace(' ', '_')}_{DateTime.Now:yyyyMMdd}.md");
        }

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
                AddPdfHeader(page, $"Llamada de Resultados: {title}");
                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Item().Background(Colors.Grey.Lighten4).Padding(8).Row(row =>
                    {
                        row.RelativeItem().Text(t =>
                        {
                            t.Span("Sentimiento: ").FontSize(10).Bold();
                            t.Span(call?.Sentiment ?? "N/A").FontSize(10);
                        });
                    });
                    col.Item().PaddingTop(8);

                    if (call?.Summary != null)
                    {
                        col.Item().Text("Resumen").Bold().FontSize(13).FontColor(BrandColor);
                        col.Item().PaddingBottom(2).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
                        RenderMarkdownToPdf(col, call.Summary);
                        col.Item().PaddingTop(8);
                    }
                    if (call?.Guidance != null)
                    {
                        col.Item().Text("Previsiones").Bold().FontSize(13).FontColor(BrandColor);
                        col.Item().PaddingBottom(2).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
                        RenderMarkdownToPdf(col, call.Guidance);
                        col.Item().PaddingTop(8);
                    }
                    if (call?.KeyMetrics != null)
                    {
                        col.Item().Text("Métricas Clave").Bold().FontSize(13).FontColor(BrandColor);
                        col.Item().PaddingBottom(2).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
                        RenderMarkdownToPdf(col, call.KeyMetrics);
                    }
                });
                AddPdfFooter(page);
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

        if (format == "md")
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# {title}");
            sb.AppendLine($"*Tipo: {report?.ReportType ?? "N/A"} · Generado: {report?.CreatedAt:dd/MM/yyyy HH:mm}*\n");
            sb.AppendLine(report?.Content ?? "Sin contenido");
            return new ExportResult(Encoding.UTF8.GetBytes(sb.ToString()), "text/markdown", $"informe_{DateTime.Now:yyyyMMdd}.md");
        }

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
                AddPdfHeader(page, title);
                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Item().Background(Colors.Grey.Lighten4).Padding(6)
                        .Text($"Tipo: {report?.ReportType ?? "N/A"}").FontSize(10).Italic().FontColor(Colors.Grey.Darken2);
                    col.Item().PaddingTop(8);
                    RenderMarkdownToPdf(col, report?.Content ?? "Sin contenido");
                });
                AddPdfFooter(page);
            });
        });
        return new ExportResult(pdf.GeneratePdf(), "application/pdf", $"informe_{DateTime.Now:yyyyMMdd}.pdf");
    }

    public ExportResult ExportBriefing(string format, string title, string content, string? sources = null)
    {
        var safeTitle = Regex.Replace(title, @"[^\w\s-]", "").Replace(' ', '_');

        if (format == "md")
        {
            var sb = new StringBuilder();
            sb.AppendLine($"# {title}");
            sb.AppendLine($"*Generado: {DateTime.Now:dd/MM/yyyy HH:mm}*\n");
            sb.AppendLine(content);
            if (!string.IsNullOrWhiteSpace(sources))
            {
                sb.AppendLine("\n---\n");
                sb.AppendLine($"**Fuentes:** {sources}");
            }
            return new ExportResult(Encoding.UTF8.GetBytes(sb.ToString()), "text/markdown", $"briefing_{safeTitle}_{DateTime.Now:yyyyMMdd}.md");
        }

        // PDF
        var pdf = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                AddPdfHeader(page, title);
                page.Content().PaddingVertical(10).Column(col =>
                {
                    RenderMarkdownToPdf(col, content);

                    if (!string.IsNullOrWhiteSpace(sources))
                    {
                        col.Item().PaddingTop(12).LineHorizontal(0.5f).LineColor(Colors.Grey.Lighten2);
                        col.Item().PaddingTop(4).Text(t =>
                        {
                            t.Span("Fuentes: ").FontSize(8).Bold().FontColor(Colors.Grey.Darken1);
                            t.Span(sources).FontSize(8).FontColor(Colors.Grey.Medium);
                        });
                    }
                });
                AddPdfFooter(page);
            });
        });
        return new ExportResult(pdf.GeneratePdf(), "application/pdf", $"briefing_{safeTitle}_{DateTime.Now:yyyyMMdd}.pdf");
    }
}
