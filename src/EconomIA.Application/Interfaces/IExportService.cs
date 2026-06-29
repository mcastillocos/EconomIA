using EconomIA.Application.DTOs;

namespace EconomIA.Application.Interfaces;

public interface IExportService
{
    Task<ExportResult> ExportFundsAsync(string format, Dictionary<string, string>? filters = null);
    Task<ExportResult> ExportPortfolioAsync(string format, string watchlistId);
    Task<ExportResult> ExportEarningsCallAsync(string format, string callId);
    Task<ExportResult> ExportReportAsync(string format, string reportId);
    ExportResult ExportBriefing(string format, string title, string content, string? sources = null);
}
