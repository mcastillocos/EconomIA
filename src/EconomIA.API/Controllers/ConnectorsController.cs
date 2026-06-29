using EconomIA.Infrastructure.Connectors;
using Microsoft.AspNetCore.Mvc;

namespace EconomIA.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConnectorsController : ControllerBase
{
    private readonly ConnectorOrchestrator _orchestrator;
    private readonly RssNewsConnector _newsConnector;
    private readonly FmpConnector _fmpConnector;
    private readonly InvestingConnector _investingConnector;

    public ConnectorsController(
        ConnectorOrchestrator orchestrator,
        RssNewsConnector newsConnector,
        FmpConnector fmpConnector,
        InvestingConnector investingConnector)
    {
        _orchestrator = orchestrator;
        _newsConnector = newsConnector;
        _fmpConnector = fmpConnector;
        _investingConnector = investingConnector;
    }

    /// <summary>
    /// Procesa un archivo subido con el conector automático según extensión.
    /// </summary>
    [HttpPost("process")]
    [ProducesResponseType(typeof(ConnectorResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ProcessFile(
        IFormFile file,
        [FromQuery] string? entityType = null,
        [FromQuery] string? entityName = null,
        [FromQuery] string? ticker = null,
        [FromQuery] string? source = null,
        CancellationToken ct = default)
    {
        if (file is null || file.Length == 0)
            return BadRequest("No se ha proporcionado un archivo válido.");

        var metadata = new ConnectorMetadata
        {
            FileName = file.FileName,
            EntityType = entityType,
            EntityName = entityName,
            Ticker = ticker,
            Source = source,
        };

        using var stream = file.OpenReadStream();
        var result = await _orchestrator.ProcessAsync(stream, metadata, ct);

        return Ok(result);
    }

    /// <summary>
    /// Obtiene las últimas noticias financieras via RSS.
    /// </summary>
    [HttpGet("news")]
    [ProducesResponseType(typeof(IReadOnlyList<NewsItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLatestNews(
        [FromQuery] int maxPerFeed = 10,
        CancellationToken ct = default)
    {
        var news = await _newsConnector.FetchLatestNewsAsync(maxPerFeed: maxPerFeed, ct: ct);
        return Ok(news);
    }

    /// <summary>
    /// Obtiene noticias filtradas por términos del watchlist.
    /// </summary>
    [HttpPost("news/filter")]
    [ProducesResponseType(typeof(IReadOnlyList<NewsItem>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFilteredNews(
        [FromBody] NewsFilterRequest request,
        CancellationToken ct = default)
    {
        var allNews = await _newsConnector.FetchLatestNewsAsync(maxPerFeed: request.MaxPerFeed, ct: ct);
        var filtered = _newsConnector.FilterByRelevance(allNews, request.Terms);
        return Ok(filtered);
    }

    /// <summary>
    /// Lista los conectores disponibles.
    /// </summary>
    [HttpGet("available")]
    [ProducesResponseType(typeof(IReadOnlyList<string>), StatusCodes.Status200OK)]
    public IActionResult GetAvailableConnectors()
    {
        return Ok(_orchestrator.GetAvailableConnectors());
    }

    /// <summary>
    /// Obtiene datos fundamentales de una empresa via Financial Modeling Prep.
    /// </summary>
    [HttpGet("fmp/{ticker}")]
    [ProducesResponseType(typeof(IReadOnlyList<NormalizedDataPoint>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFmpData(string ticker, CancellationToken ct)
    {
        var data = await _fmpConnector.FetchCompanyDataAsync(ticker.ToUpperInvariant(), ct);
        return Ok(data);
    }

    /// <summary>
    /// Obtiene cotización en tiempo real via FMP.
    /// </summary>
    [HttpGet("fmp/{ticker}/quote")]
    [ProducesResponseType(typeof(FmpQuote), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFmpQuote(string ticker, CancellationToken ct)
    {
        var quote = await _fmpConnector.FetchQuoteAsync(ticker.ToUpperInvariant(), ct);
        if (quote is null) return NotFound();
        return Ok(quote);
    }

    /// <summary>
    /// Obtiene calendario económico de Investing.com.
    /// </summary>
    [HttpGet("investing/calendar")]
    [ProducesResponseType(typeof(IReadOnlyList<EconomicEvent>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEconomicCalendar(CancellationToken ct)
    {
        var events = await _investingConnector.FetchEconomicCalendarAsync(ct);
        return Ok(events);
    }
}

public record NewsFilterRequest
{
    public IReadOnlyList<string> Terms { get; init; } = [];
    public int MaxPerFeed { get; init; } = 10;
}
