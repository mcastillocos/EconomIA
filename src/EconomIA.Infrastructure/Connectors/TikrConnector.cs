using System.Globalization;

namespace EconomIA.Infrastructure.Connectors;

/// <summary>
/// Conector real para datos exportados de Tikr.com.
/// Parsea los CSV/Excel descargados por el usuario con estados financieros:
/// Income Statement, Balance Sheet, Cash Flow Statement.
/// 
/// Formato típico Tikr export:
///   Fila 0: ticker / nombre empresa
///   Fila 1: headers con años (FY 2020, FY 2021, FY 2022, ...)
///   Filas 2+: métricas (Revenue, EBITDA, Net Income, etc.) con valores por año
/// </summary>
public class TikrConnector : IDataConnector
{
    public string ConnectorName => "tikr_connector";
    public string[] SupportedFileTypes => ["csv", "xlsx", "xls"];

    // Métricas reconocidas de Tikr exports (Income Statement)
    private static readonly Dictionary<string, string> MetricMappings = new(StringComparer.OrdinalIgnoreCase)
    {
        // Income Statement
        ["Revenue"] = "Revenue",
        ["Total Revenue"] = "Revenue",
        ["Ingresos"] = "Revenue",
        ["Ingresos totales"] = "Revenue",
        ["Cost of Revenue"] = "COGS",
        ["Cost of Goods Sold"] = "COGS",
        ["Gross Profit"] = "GrossProfit",
        ["Beneficio bruto"] = "GrossProfit",
        ["Operating Income"] = "OperatingIncome",
        ["Operating Profit"] = "OperatingIncome",
        ["EBIT"] = "EBIT",
        ["EBITDA"] = "EBITDA",
        ["Net Income"] = "NetIncome",
        ["Beneficio neto"] = "NetIncome",
        ["Net Income Common"] = "NetIncome",
        ["EPS"] = "EPS",
        ["EPS (Diluted)"] = "EPSDiluted",
        ["Diluted EPS"] = "EPSDiluted",
        ["Shares Outstanding"] = "SharesOutstanding",
        ["Diluted Shares Outstanding"] = "DilutedShares",
        ["Dividend Per Share"] = "DividendPerShare",
        ["Interest Expense"] = "InterestExpense",
        ["Tax Expense"] = "TaxExpense",
        ["Income Tax Expense"] = "TaxExpense",
        ["SGA"] = "SGA",
        ["Selling General & Admin"] = "SGA",
        ["R&D Expense"] = "RDExpense",
        ["Research & Development"] = "RDExpense",
        ["Depreciation & Amortization"] = "DepreciationAmortization",

        // Balance Sheet
        ["Total Assets"] = "TotalAssets",
        ["Total Liabilities"] = "TotalLiabilities",
        ["Total Equity"] = "TotalEquity",
        ["Shareholders Equity"] = "TotalEquity",
        ["Total Debt"] = "TotalDebt",
        ["Long Term Debt"] = "LongTermDebt",
        ["Short Term Debt"] = "ShortTermDebt",
        ["Cash and Equivalents"] = "CashAndEquivalents",
        ["Cash & Cash Equivalents"] = "CashAndEquivalents",
        ["Total Current Assets"] = "CurrentAssets",
        ["Total Current Liabilities"] = "CurrentLiabilities",
        ["Accounts Receivable"] = "AccountsReceivable",
        ["Inventory"] = "Inventory",
        ["Goodwill"] = "Goodwill",
        ["Intangible Assets"] = "IntangibleAssets",
        ["Book Value Per Share"] = "BookValuePerShare",
        ["Tangible Book Value"] = "TangibleBookValue",

        // Cash Flow
        ["Operating Cash Flow"] = "OperatingCashFlow",
        ["Cash from Operations"] = "OperatingCashFlow",
        ["Capital Expenditure"] = "CapEx",
        ["Capital Expenditures"] = "CapEx",
        ["Free Cash Flow"] = "FreeCashFlow",
        ["FCF"] = "FreeCashFlow",
        ["Dividends Paid"] = "DividendsPaid",
        ["Share Repurchase"] = "ShareRepurchase",
        ["Stock Buyback"] = "ShareRepurchase",

        // Ratios
        ["Gross Margin"] = "GrossMargin",
        ["Operating Margin"] = "OperatingMargin",
        ["Net Margin"] = "NetMargin",
        ["Profit Margin"] = "NetMargin",
        ["ROE"] = "ROE",
        ["Return on Equity"] = "ROE",
        ["ROA"] = "ROA",
        ["Return on Assets"] = "ROA",
        ["ROIC"] = "ROIC",
        ["Return on Invested Capital"] = "ROIC",
        ["Current Ratio"] = "CurrentRatio",
        ["Debt to Equity"] = "DebtToEquity",
        ["P/E Ratio"] = "PERatio",
        ["Price to Earnings"] = "PERatio",
        ["P/B Ratio"] = "PBRatio",
        ["EV/EBITDA"] = "EVToEBITDA",
        ["Payout Ratio"] = "PayoutRatio",
    };

    public async Task<IReadOnlyList<NormalizedDataPoint>> ExtractAsync(Stream stream, ConnectorMetadata metadata, CancellationToken ct = default)
    {
        var results = new List<NormalizedDataPoint>();

        using var reader = new StreamReader(stream);
        var allLines = new List<string>();
        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync(ct);
            if (line is not null) allLines.Add(line);
        }

        if (allLines.Count < 3) return results;

        // Detectar separador
        var separator = DetectSeparator(allLines[0]);

        // Buscar la fila de headers con años (FY 2020, 2021, etc.)
        var (headerRowIdx, years) = FindYearHeaders(allLines, separator);
        if (headerRowIdx < 0 || years.Length == 0) return results;

        // Extraer ticker/nombre de la primera fila si disponible
        var ticker = metadata.Ticker ?? ExtractTicker(allLines, separator, headerRowIdx);

        // Parsear métricas
        for (int i = headerRowIdx + 1; i < allLines.Count; i++)
        {
            ct.ThrowIfCancellationRequested();
            var parts = ParseLine(allLines[i], separator);
            if (parts.Length < 2) continue;

            var rawMetricName = parts[0].Trim().Trim('"');
            if (string.IsNullOrWhiteSpace(rawMetricName)) continue;

            var normalizedMetric = NormalizeMetric(rawMetricName);
            if (normalizedMetric is null) continue; // Métrica no reconocida, skip

            for (int col = 1; col < parts.Length && col - 1 < years.Length; col++)
            {
                var valueStr = parts[col].Trim().Trim('"').Replace(",", "").Replace(" ", "");

                // Manejar paréntesis como negativos: (1234) → -1234
                if (valueStr.StartsWith('(') && valueStr.EndsWith(')'))
                    valueStr = "-" + valueStr[1..^1];

                // Manejar porcentajes: 25.3% → 25.3
                valueStr = valueStr.TrimEnd('%');

                if (string.IsNullOrWhiteSpace(valueStr) || valueStr == "-" || valueStr == "N/A")
                    continue;

                if (decimal.TryParse(valueStr, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
                {
                    results.Add(new NormalizedDataPoint
                    {
                        Source = "tikr.com",
                        SourceType = "api",
                        EntityType = metadata.EntityType ?? "company",
                        EntityName = metadata.EntityName ?? ticker ?? "Unknown",
                        Ticker = ticker,
                        Isin = metadata.Isin,
                        Metric = normalizedMetric,
                        Value = value,
                        Year = years[col - 1],
                        Currency = DetectCurrency(allLines, separator, headerRowIdx),
                        FileName = metadata.FileName,
                        RetrievedAt = DateTime.UtcNow,
                        Confidence = "high",
                        RawReference = rawMetricName,
                    });
                }
            }
        }

        return results;
    }

    private static string? NormalizeMetric(string rawName)
    {
        // Buscar coincidencia exacta
        if (MetricMappings.TryGetValue(rawName, out var mapped))
            return mapped;

        // Buscar por contenido parcial
        foreach (var (key, value) in MetricMappings)
        {
            if (rawName.Contains(key, StringComparison.OrdinalIgnoreCase))
                return value;
        }

        return null;
    }

    private static (int rowIndex, int[] years) FindYearHeaders(List<string> lines, char separator)
    {
        for (int i = 0; i < Math.Min(lines.Count, 5); i++)
        {
            var parts = ParseLine(lines[i], separator);
            var years = new List<int>();

            foreach (var part in parts.Skip(1))
            {
                var cleaned = part.Trim().Trim('"')
                    .Replace("FY ", "").Replace("FY", "")
                    .Replace("CY ", "").Replace("CY", "")
                    .Replace("TTM", "").Trim();

                if (int.TryParse(cleaned, out var year) && year >= 2000 && year <= 2030)
                    years.Add(year);
            }

            if (years.Count >= 1)
                return (i, years.ToArray());
        }

        return (-1, []);
    }

    private static string? ExtractTicker(List<string> lines, char separator, int headerRow)
    {
        for (int i = 0; i < headerRow; i++)
        {
            var parts = ParseLine(lines[i], separator);
            if (parts.Length > 0)
            {
                var candidate = parts[0].Trim().Trim('"');
                // Tickers suelen ser 1-5 letras mayúsculas
                if (candidate.Length is >= 1 and <= 6 && candidate.All(c => char.IsLetterOrDigit(c) || c == '.'))
                    return candidate.ToUpperInvariant();
            }
        }
        return null;
    }

    private static string? DetectCurrency(List<string> lines, char separator, int headerRow)
    {
        var searchArea = string.Join(" ", lines.Take(headerRow + 1));
        if (searchArea.Contains("USD", StringComparison.OrdinalIgnoreCase) || searchArea.Contains("$"))
            return "USD";
        if (searchArea.Contains("EUR", StringComparison.OrdinalIgnoreCase) || searchArea.Contains("€"))
            return "EUR";
        if (searchArea.Contains("GBP", StringComparison.OrdinalIgnoreCase) || searchArea.Contains("£"))
            return "GBP";
        return null;
    }

    private static char DetectSeparator(string line)
    {
        var tabCount = line.Count(c => c == '\t');
        var commaCount = line.Count(c => c == ',');
        var semicolonCount = line.Count(c => c == ';');

        if (tabCount > commaCount && tabCount > semicolonCount) return '\t';
        if (semicolonCount > commaCount) return ';';
        return ',';
    }

    private static string[] ParseLine(string line, char separator)
    {
        if (separator == ',')
        {
            var parts = new List<string>();
            var inQuotes = false;
            var current = new System.Text.StringBuilder();

            foreach (var c in line)
            {
                if (c == '"') { inQuotes = !inQuotes; continue; }
                if (c == ',' && !inQuotes) { parts.Add(current.ToString()); current.Clear(); continue; }
                current.Append(c);
            }
            parts.Add(current.ToString());
            return parts.ToArray();
        }

        return line.Split(separator);
    }
}
