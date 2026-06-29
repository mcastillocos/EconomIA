using EconomIA.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EconomIA.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExportController : ControllerBase
{
    private readonly IExportService _export;

    public ExportController(IExportService export)
    {
        _export = export;
    }

    [HttpGet("funds")]
    public async Task<IActionResult> ExportFunds([FromQuery] string format = "pdf")
    {
        var result = await _export.ExportFundsAsync(format);
        return File(result.FileContent, result.ContentType, result.FileName);
    }

    [HttpGet("portfolio/{watchlistId}")]
    public async Task<IActionResult> ExportPortfolio(string watchlistId, [FromQuery] string format = "pdf")
    {
        var result = await _export.ExportPortfolioAsync(format, watchlistId);
        return File(result.FileContent, result.ContentType, result.FileName);
    }

    [HttpGet("earnings-call/{callId}")]
    public async Task<IActionResult> ExportEarningsCall(string callId, [FromQuery] string format = "pdf")
    {
        var result = await _export.ExportEarningsCallAsync(format, callId);
        return File(result.FileContent, result.ContentType, result.FileName);
    }

    [HttpGet("report/{reportId}")]
    public async Task<IActionResult> ExportReport(string reportId, [FromQuery] string format = "pdf")
    {
        var result = await _export.ExportReportAsync(format, reportId);
        return File(result.FileContent, result.ContentType, result.FileName);
    }
}
