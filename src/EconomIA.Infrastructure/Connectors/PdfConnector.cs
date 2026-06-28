using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;

namespace EconomIA.Infrastructure.Connectors;

/// <summary>
/// Basic PDF connector that extracts text content from PDFs.
/// Uses PdfPig for text extraction.
/// </summary>
public partial class PdfConnector : IDataConnector
{
    public string ConnectorName => "pdf_connector";
    public string[] SupportedFileTypes => ["pdf"];

    public async Task<IReadOnlyList<NormalizedDataPoint>> ExtractAsync(Stream stream, ConnectorMetadata metadata, CancellationToken ct = default)
    {
        var results = new List<NormalizedDataPoint>();

        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, ct);
        ms.Position = 0;

        using var document = PdfDocument.Open(ms);

        foreach (var page in document.GetPages())
        {
            ct.ThrowIfCancellationRequested();

            var text = page.Text;
            if (string.IsNullOrWhiteSpace(text)) continue;

            // Extract numbers that look like financial metrics
            var matches = FinancialNumberRegex().Matches(text);

            foreach (Match match in matches)
            {
                // Try to find context around the number (preceding label)
                var position = match.Index;
                var precedingText = text[Math.Max(0, position - 80)..position].Trim();
                var label = ExtractLabel(precedingText);

                if (!string.IsNullOrWhiteSpace(label) && decimal.TryParse(
                    match.Value.Replace(",", "").Replace(" ", ""),
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var value))
                {
                    results.Add(new NormalizedDataPoint
                    {
                        Source = metadata.Source ?? metadata.FileName,
                        SourceType = "pdf",
                        EntityType = metadata.EntityType ?? "company",
                        EntityName = metadata.EntityName ?? "",
                        Ticker = metadata.Ticker,
                        Isin = metadata.Isin,
                        Metric = label,
                        Value = value,
                        FileName = metadata.FileName,
                        Page = page.Number.ToString(),
                        RetrievedAt = DateTime.UtcNow,
                        Confidence = "low",
                        RawText = text[Math.Max(0, position - 40)..Math.Min(text.Length, position + match.Length + 20)]
                    });
                }
            }
        }

        return await Task.FromResult<IReadOnlyList<NormalizedDataPoint>>(results);
    }

    private static string ExtractLabel(string text)
    {
        // Take the last meaningful fragment as label
        var lines = text.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries);
        var lastLine = lines.Length > 0 ? lines[^1].Trim() : text;

        // Remove trailing colons, dots, etc.
        lastLine = lastLine.TrimEnd(':', '.', ' ', '\t');

        // Limit label length
        if (lastLine.Length > 60)
            lastLine = lastLine[^60..];

        return lastLine;
    }

    [GeneratedRegex(@"-?\d{1,3}(?:[,. ]\d{3})*(?:\.\d+)?", RegexOptions.Compiled)]
    private static partial Regex FinancialNumberRegex();
}
