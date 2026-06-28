using System.Globalization;

namespace EconomIA.Infrastructure.Connectors;

public class CsvConnector : IDataConnector
{
    public string ConnectorName => "csv_connector";
    public string[] SupportedFileTypes => ["csv", "tsv"];

    public async Task<IReadOnlyList<NormalizedDataPoint>> ExtractAsync(Stream stream, ConnectorMetadata metadata, CancellationToken ct = default)
    {
        var results = new List<NormalizedDataPoint>();

        using var reader = new StreamReader(stream);
        var headerLine = await reader.ReadLineAsync(ct);
        if (string.IsNullOrWhiteSpace(headerLine))
            return results;

        var separator = DetectSeparator(headerLine);
        var headers = headerLine.Split(separator).Select(h => h.Trim().Trim('"').ToLowerInvariant()).ToArray();

        var rowNumber = 1;
        string? line;
        while ((line = await reader.ReadLineAsync(ct)) is not null)
        {
            ct.ThrowIfCancellationRequested();
            if (string.IsNullOrWhiteSpace(line))
            {
                rowNumber++;
                continue;
            }

            var values = ParseCsvLine(line, separator);
            rowNumber++;

            var record = MapToDict(headers, values);

            // Try to extract metric/value pairs
            var metricName = GetValue(record, "metric", "metrica", "indicador", "kpi", "metric_name");
            var valueStr = GetValue(record, "value", "valor", "amount", "importe");

            if (!string.IsNullOrWhiteSpace(metricName) && decimal.TryParse(valueStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var numericValue))
            {
                results.Add(new NormalizedDataPoint
                {
                    Source = metadata.Source ?? metadata.FileName,
                    SourceType = "csv",
                    EntityType = metadata.EntityType ?? GetValue(record, "entity_type", "tipo_entidad") ?? "company",
                    EntityName = metadata.EntityName ?? GetValue(record, "entity_name", "nombre", "company", "empresa", "fund", "fondo") ?? "",
                    Ticker = metadata.Ticker ?? GetValue(record, "ticker", "symbol", "simbolo"),
                    Isin = metadata.Isin ?? GetValue(record, "isin"),
                    Market = GetValue(record, "market", "mercado"),
                    Country = GetValue(record, "country", "pais", "país"),
                    Sector = GetValue(record, "sector"),
                    Industry = GetValue(record, "industry", "industria"),
                    Metric = metricName,
                    Value = numericValue,
                    Period = GetValue(record, "period", "periodo"),
                    Year = ParseInt(GetValue(record, "year", "año", "anio")),
                    Quarter = ParseInt(GetValue(record, "quarter", "trimestre", "q")),
                    Currency = GetValue(record, "currency", "moneda", "divisa"),
                    FileName = metadata.FileName,
                    Row = rowNumber.ToString(),
                    RetrievedAt = DateTime.UtcNow,
                    Confidence = "high",
                    RawText = line
                });
            }
            else
            {
                // If no explicit metric/value columns, try each numeric column as a metric
                foreach (var header in headers)
                {
                    var idx = Array.IndexOf(headers, header);
                    if (idx < values.Length && decimal.TryParse(values[idx], NumberStyles.Any, CultureInfo.InvariantCulture, out var val))
                    {
                        // Skip known non-metric columns
                        if (IsNonMetricColumn(header)) continue;

                        results.Add(new NormalizedDataPoint
                        {
                            Source = metadata.Source ?? metadata.FileName,
                            SourceType = "csv",
                            EntityType = metadata.EntityType ?? "company",
                            EntityName = metadata.EntityName ?? GetValue(record, "name", "nombre", "company", "empresa", "fund", "fondo", "ticker") ?? "",
                            Ticker = metadata.Ticker ?? GetValue(record, "ticker", "symbol"),
                            Isin = metadata.Isin ?? GetValue(record, "isin"),
                            Metric = header,
                            Value = val,
                            Year = ParseInt(GetValue(record, "year", "año", "anio")),
                            Quarter = ParseInt(GetValue(record, "quarter", "trimestre", "q")),
                            Currency = GetValue(record, "currency", "moneda", "divisa"),
                            FileName = metadata.FileName,
                            Row = rowNumber.ToString(),
                            RetrievedAt = DateTime.UtcNow,
                            Confidence = "medium",
                            RawText = line
                        });
                    }
                }
            }
        }

        return results;
    }

    private static char DetectSeparator(string headerLine)
    {
        if (headerLine.Contains('\t')) return '\t';
        if (headerLine.Contains(';')) return ';';
        return ',';
    }

    private static string[] ParseCsvLine(string line, char separator)
    {
        var fields = new List<string>();
        var inQuotes = false;
        var field = "";

        foreach (var ch in line)
        {
            if (ch == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (ch == separator && !inQuotes)
            {
                fields.Add(field.Trim());
                field = "";
            }
            else
            {
                field += ch;
            }
        }
        fields.Add(field.Trim());
        return fields.ToArray();
    }

    private static Dictionary<string, string> MapToDict(string[] headers, string[] values)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < headers.Length && i < values.Length; i++)
        {
            dict[headers[i]] = values[i];
        }
        return dict;
    }

    private static string? GetValue(Dictionary<string, string> record, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (record.TryGetValue(key, out var val) && !string.IsNullOrWhiteSpace(val))
                return val.Trim('"');
        }
        return null;
    }

    private static int? ParseInt(string? value)
    {
        if (int.TryParse(value, out var result)) return result;
        return null;
    }

    private static bool IsNonMetricColumn(string header)
    {
        var nonMetric = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "name", "nombre", "company", "empresa", "fund", "fondo", "ticker", "symbol",
            "isin", "market", "mercado", "country", "pais", "país", "sector", "industry",
            "industria", "currency", "moneda", "divisa", "entity_type", "tipo_entidad",
            "period", "periodo", "year", "año", "anio", "quarter", "trimestre", "q",
            "date", "fecha", "id"
        };
        return nonMetric.Contains(header);
    }
}
