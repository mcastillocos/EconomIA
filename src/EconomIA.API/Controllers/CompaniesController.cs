using System.Text.Json;
using EconomIA.Application.DTOs;
using EconomIA.Application.Interfaces;
using EconomIA.Domain.Entities;
using EconomIA.Domain.Ports;
using Microsoft.AspNetCore.Mvc;

namespace EconomIA.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CompaniesController : ControllerBase
{
    private readonly ICompanyRepository _repository;
    private readonly IServiceProvider _sp;

    public CompaniesController(ICompanyRepository repository, IServiceProvider sp)
    {
        _repository = repository;
        _sp = sp;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<CompanyDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var companies = await _repository.GetAllAsync(ct);
        var dtos = companies.Select(MapToDto).ToList();
        return Ok(dtos);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CompanyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var company = await _repository.GetByIdAsync(id, ct);
        return company is null ? NotFound() : Ok(MapToDto(company));
    }

    [HttpPost]
    [ProducesResponseType(typeof(CompanyDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateCompanyRequest request, CancellationToken ct)
    {
        var company = Company.Create(
            request.Name,
            request.Ticker,
            request.Isin,
            request.Market,
            request.Country,
            request.Sector,
            request.Industry,
            request.Currency,
            request.Competitors,
            request.RelevantUrls,
            request.PreferredSource,
            request.Notes);

        await _repository.AddAsync(company, ct);
        await _repository.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = company.Id }, MapToDto(company));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(CompanyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCompanyRequest request, CancellationToken ct)
    {
        var company = await _repository.GetByIdAsync(id, ct);
        if (company is null) return NotFound();

        company.Update(
            request.Name,
            request.Ticker,
            request.Isin,
            request.Market,
            request.Country,
            request.Sector,
            request.Industry,
            request.Currency,
            request.Competitors,
            request.RelevantUrls,
            request.PreferredSource,
            request.Notes);

        await _repository.UpdateAsync(company, ct);
        await _repository.SaveChangesAsync(ct);

        return Ok(MapToDto(company));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var company = await _repository.GetByIdAsync(id, ct);
        if (company is null) return NotFound();

        await _repository.DeleteAsync(id, ct);
        await _repository.SaveChangesAsync(ct);

        return NoContent();
    }

    private static CompanyDto MapToDto(Company c) => new(
        c.Id, c.Name, c.Ticker, c.Isin, c.Market, c.Country,
        c.Sector, c.Industry, c.Currency, c.Competitors,
        c.RelevantUrls, c.PreferredSource, c.Notes,
        c.CreatedAt, c.UpdatedAt);

    /// <summary>
    /// Busca datos de una empresa por nombre/ticker usando IA.
    /// Devuelve un CreateCompanyRequest prellenado SIN guardar.
    /// </summary>
    [HttpPost("ai-lookup")]
    [ProducesResponseType(typeof(CreateCompanyRequest), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> AiLookup([FromBody] AiLookupRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
            return BadRequest(new { error = "Se requiere un nombre o ticker de empresa." });

        ILlmService llm;
        try
        {
            llm = _sp.GetRequiredService<ILlmService>();
        }
        catch
        {
            return StatusCode(503, new { error = "Servicio de IA no disponible. Configure las API keys en appsettings." });
        }

        var systemPrompt = """
            Eres un experto en mercados financieros. Dado un nombre o ticker de empresa,
            devuelve SOLO un JSON con estos campos (sin texto adicional, sin markdown):
            {
              "name": "Nombre completo de la empresa",
              "ticker": "Ticker principal (ej: AAPL)",
              "isin": "Código ISIN si lo conoces, o null",
              "market": "Mercado principal (ej: NASDAQ, NYSE, BME, LSE, XETRA)",
              "country": "País de sede (ej: Estados Unidos, España)",
              "sector": "Sector (ej: Tecnología, Energía, Salud)",
              "industry": "Industria específica (ej: Semiconductores, Software)",
              "currency": "Divisa principal (ej: USD, EUR, GBP)",
              "competitors": "Principales competidores separados por comas",
              "relevantUrls": "Web oficial y fuentes financieras relevantes separadas por comas",
              "preferredSource": "Mejor fuente de datos financieros para esta empresa",
              "notes": "Breve descripción de la empresa y datos clave (capitalización, empleados, año fundación)"
            }
            Si no conoces un campo con certeza, usa null. No inventes datos.
            """;

        try
        {
            var response = await llm.ChatAsync(systemPrompt, $"Busca información de: {request.Query}", new LlmOptions { MaxTokens = 1000 }, ct);

            var content = response.Content.Trim();
            // Limpiar posible markdown fence
            if (content.StartsWith("```"))
            {
                var firstNewline = content.IndexOf('\n');
                var lastFence = content.LastIndexOf("```");
                if (firstNewline > 0 && lastFence > firstNewline)
                    content = content[(firstNewline + 1)..lastFence].Trim();
            }

            var result = JsonSerializer.Deserialize<CreateCompanyRequest>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result is null || string.IsNullOrWhiteSpace(result.Name))
                return BadRequest(new { error = "La IA no pudo identificar la empresa.", raw = content });

            return Ok(result);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("LLM providers"))
        {
            return StatusCode(503, new { error = "No hay proveedores LLM configurados. Configure las API keys de Azure OpenAI o Claude.", detail = ex.Message });
        }
        catch (JsonException)
        {
            return BadRequest(new { error = "La IA devolvió un formato inesperado. Intente de nuevo." });
        }
    }
}

public record AiLookupRequest(string Query);
