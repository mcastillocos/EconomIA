using System.Globalization;

namespace EconomIA.Infrastructure.Connectors;

/// <summary>
/// Excel connector using basic OpenXML-based parsing.
/// Requires ClosedXML NuGet package.
/// </summary>
public class ExcelConnector : IDataConnector
{
    public string ConnectorName => "excel_connector";
    public string[] SupportedFileTypes => ["xlsx", "xls"];

    public async Task<IReadOnlyList<NormalizedDataPoint>> ExtractAsync(Stream stream, ConnectorMetadata metadata, CancellationToken ct = default)
    {
        var results = new List<NormalizedDataPoint>();

        // Copy stream to MemoryStream for ClosedXML (needs seekable stream)
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, ct);
        ms.Position = 0;

        using var workbook = new ClosedXML.Excel.XLWorkbook(ms);

        foreach (var worksheet in workbook.Worksheets)
        {
            ct.ThrowIfCancellationRequested();

            var usedRange = worksheet.RangeUsed();
            if (usedRange is null) continue;

            var firstRow = usedRange.FirstRow().RowNumber();
            var lastRow = usedRange.LastRow().RowNumber();
            var firstCol = usedRange.FirstColumn().ColumnNumber();
            var lastCol = usedRange.LastColumn().ColumnNumber();

            // Read headers from first row
            var headers = new string[lastCol - firstCol + 1];
            for (int col = firstCol; col <= lastCol; col++)
            {
                headers[col - firstCol] = worksheet.Cell(firstRow, col).GetString().Trim().ToLowerInvariant();
            }

            // Read data rows
            for (int row = firstRow + 1; row <= lastRow; row++)
            {
                ct.ThrowIfCancellationRequested();

                var values = new string[headers.Length];
                for (int col = firstCol; col <= lastCol; col++)
                {
                    values[col - firstCol] = worksheet.Cell(row, col).GetString().Trim();
                }

                var record = MapToDict(headers, values);

                var metricName = GetValue(record, "metric", "metrica", "indicador", "kpi");
                var valueStr = GetValue(record, "value", "valor", "amount", "importe");

                if (!string.IsNullOrWhiteSpace(metricName) && decimal.TryParse(valueStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var numericValue))
                {
                    results.Add(new NormalizedDataPoint
                    {
                        Source = metadata.Source ?? metadata.FileName,
                        SourceType = "excel",
                        EntityType = metadata.EntityType ?? GetValue(record, "entity_type", "tipo_entidad") ?? "company",
                        EntityName = metadata.EntityName ?? GetValue(record, "entity_name", "nombre", "company", "empresa") ?? "",
                        Ticker = metadata.Ticker ?? GetValue(record, "ticker", "symbol"),
                        Isin = metadata.Isin ?? GetValue(record, "isin"),
                        Market = GetValue(record, "market", "mercado"),
                        Country = GetValue(record, "country", "pais"),
                        Sector = GetValue(record, "sector"),
                        Industry = GetValue(record, "industry", "industria"),
                        Metric = metricName,
                        Value = numericValue,
                        Period = GetValue(record, "period", "periodo"),
                        Year = ParseInt(GetValue(record, "year", "año", "anio")),
                        Quarter = ParseInt(GetValue(record, "quarter", "trimestre", "q")),
                        Currency = GetValue(record, "currency", "moneda", "divisa"),
                        FileName = metadata.FileName,
                        Page = worksheet.Name,
                        Row = row.ToString(),
                        RetrievedAt = DateTime.UtcNow,
                        Confidence = "high",
                        RawText = string.Join(" | ", values)
                    });
                }
                else
                {
                    // Each numeric column as metric
                    for (int i = 0; i < headers.Length; i++)
                    {
                        if (IsNonMetricColumn(headers[i])) continue;
                        if (decimal.TryParse(values[i], NumberStyles.Any, CultureInfo.InvariantCulture, out var val))
                        {
                            results.Add(new NormalizedDataPoint
                            {
                                Source = metadata.Source ?? metadata.FileName,
                                SourceType = "excel",
                                EntityType = metadata.EntityType ?? "company",
                                EntityName = metadata.EntityName ?? GetValue(record, "name", "nombre", "company", "empresa", "ticker") ?? "",
                                Ticker = metadata.Ticker ?? GetValue(record, "ticker", "symbol"),
                                Isin = metadata.Isin ?? GetValue(record, "isin"),
                                Metric = headers[i],
                                Value = val,
                                Year = ParseInt(GetValue(record, "year", "año", "anio")),
                                Quarter = ParseInt(GetValue(record, "quarter", "trimestre", "q")),
                                Currency = GetValue(record, "currency", "moneda", "divisa"),
                                FileName = metadata.FileName,
                                Page = worksheet.Name,
                                Row = row.ToString(),
                                RetrievedAt = DateTime.UtcNow,
                                Confidence = "medium",
                                RawText = string.Join(" | ", values)
                            });
                        }
                    }
                }
            }
        }

        return results;
    }

    private static Dictionary<string, string> MapToDict(string[] headers, string[] values)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < headers.Length && i < values.Length; i++)
            dict[headers[i]] = values[i];
        return dict;
    }

    private static string? GetValue(Dictionary<string, string> record, params string[] keys)
    {
        foreach (var key in keys)
            if (record.TryGetValue(key, out var val) && !string.IsNullOrWhiteSpace(val))
                return val.Trim('"');
        return null;
    }

    private static int? ParseInt(string? value) =>
        int.TryParse(value, out var r) ? r : null;

    private static bool IsNonMetricColumn(string header)
    {
        var nonMetric = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "name", "nombre", "company", "empresa", "fund", "fondo", "ticker", "symbol",
            "isin", "market", "mercado", "country", "pais", "sector", "industry",
            "industria", "currency", "moneda", "divisa", "entity_type", "tipo_entidad",
            "period", "periodo", "year", "año", "anio", "quarter", "trimestre", "q",
            "date", "fecha", "id"
        };
        return nonMetric.Contains(header);
    }
}
