using System.Net;
using System.Net.Http.Json;
using EconomIA.Application.DTOs;
using EconomIA.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace EconomIA.API.Tests.Controllers;

public class CompaniesControllerTests
{
    private EconomIAWebFactory CreateFactory(ILlmService? llmService = null)
    {
        var factory = new EconomIAWebFactory();
        if (llmService is not null)
        {
            factory.WithLlmService(llmService);
        }
        return factory;
    }

    // ---- CRUD básico ----

    [Fact]
    public async Task GetAll_SinEmpresas_DevuelveListaVacia()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/companies");

        response.EnsureSuccessStatusCode();
        var companies = await response.Content.ReadFromJsonAsync<List<CompanyDto>>();
        Assert.NotNull(companies);
        Assert.Empty(companies);
    }

    [Fact]
    public async Task Create_EmpresaValida_Devuelve201()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();

        var request = new CreateCompanyRequest("Apple Inc.", "AAPL", null, "NASDAQ",
            "Estados Unidos", "Tecnología", "Consumer Electronics", "USD",
            "Microsoft, Google", null, null, null);

        var response = await client.PostAsJsonAsync("/api/companies", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var dto = await response.Content.ReadFromJsonAsync<CompanyDto>();
        Assert.NotNull(dto);
        Assert.Equal("Apple Inc.", dto.Name);
        Assert.Equal("AAPL", dto.Ticker);
        Assert.Equal("NASDAQ", dto.Market);
    }

    [Fact]
    public async Task Create_NombreVacio_Devuelve400()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();

        var request = new CreateCompanyRequest("", null, null, null, null, null, null, null, null, null, null, null);

        var response = await client.PostAsJsonAsync("/api/companies", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Delete_Existente_Devuelve204()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();

        var createReq = new CreateCompanyRequest("Test Delete", "TST", null, null, null, null, null, null, null, null, null, null);
        var createResp = await client.PostAsJsonAsync("/api/companies", createReq);
        var created = await createResp.Content.ReadFromJsonAsync<CompanyDto>();

        var response = await client.DeleteAsync($"/api/companies/{created!.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Delete_NoExiste_Devuelve404()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();

        var response = await client.DeleteAsync($"/api/companies/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ---- AI Lookup ----

    [Fact]
    public async Task AiLookup_QueryVacio_Devuelve400()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/companies/ai-lookup", new { query = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AiLookup_SinLlmConfigurado_Devuelve503()
    {
        // Registrar un LLM fake que simula "no hay providers"
        var noProvidersLlm = new FakeLlmService(throwNoProviders: true);
        await using var factory = CreateFactory(noProvidersLlm);
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/companies/ai-lookup", new { query = "Apple" });

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    [Fact]
    public async Task AiLookup_RespuestaValida_DevuelveCompanyPrellenada()
    {
        var fakeLlm = new FakeLlmService("""
            {
              "name": "Apple Inc.",
              "ticker": "AAPL",
              "isin": "US0378331005",
              "market": "NASDAQ",
              "country": "Estados Unidos",
              "sector": "Tecnología",
              "industry": "Consumer Electronics",
              "currency": "USD",
              "competitors": "Microsoft, Google, Samsung",
              "relevantUrls": "https://apple.com",
              "preferredSource": "Yahoo Finance",
              "notes": "Fundada en 1976. Cap. bursátil ~3T USD."
            }
            """);

        await using var factory = CreateFactory(fakeLlm);
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/companies/ai-lookup", new { query = "Apple" });

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CreateCompanyRequest>();
        Assert.NotNull(result);
        Assert.Equal("Apple Inc.", result.Name);
        Assert.Equal("AAPL", result.Ticker);
        Assert.Equal("NASDAQ", result.Market);
        Assert.Equal("Tecnología", result.Sector);
        Assert.Equal("USD", result.Currency);
    }

    [Fact]
    public async Task AiLookup_RespuestaConMarkdownFence_LimpiaYParsea()
    {
        var fakeLlm = new FakeLlmService("""
            ```json
            {
              "name": "Tesla Inc.",
              "ticker": "TSLA",
              "market": "NASDAQ",
              "country": "Estados Unidos",
              "sector": "Automoción"
            }
            ```
            """);

        await using var factory = CreateFactory(fakeLlm);
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/companies/ai-lookup", new { query = "Tesla" });

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CreateCompanyRequest>();
        Assert.NotNull(result);
        Assert.Equal("Tesla Inc.", result.Name);
        Assert.Equal("TSLA", result.Ticker);
    }

    [Fact]
    public async Task AiLookup_RespuestaInvalida_Devuelve400()
    {
        var fakeLlm = new FakeLlmService("No puedo encontrar esa empresa, lo siento.");

        await using var factory = CreateFactory(fakeLlm);
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/companies/ai-lookup", new { query = "xyzabc123" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AiLookup_LlmSinProveedores_Devuelve503()
    {
        var fakeLlm = new FakeLlmService(throwNoProviders: true);

        await using var factory = CreateFactory(fakeLlm);
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/companies/ai-lookup", new { query = "Apple" });

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }
}

/// <summary>
/// Fake LLM que devuelve una respuesta fija o lanza excepción.
/// </summary>
internal class FakeLlmService : ILlmService
{
    private readonly string? _response;
    private readonly bool _throwNoProviders;

    public FakeLlmService(string response) => _response = response;
    public FakeLlmService(bool throwNoProviders = false) => _throwNoProviders = throwNoProviders;

    public Task<LlmResponse> ChatAsync(string systemPrompt, string userPrompt, LlmOptions? options = null, CancellationToken ct = default)
    {
        if (_throwNoProviders)
            throw new InvalidOperationException("No LLM providers configured.");

        return Task.FromResult(new LlmResponse
        {
            Content = _response ?? "",
            Provider = "Fake",
            Model = "fake-1",
            PromptTokens = 10,
            CompletionTokens = 50,
            TotalTokens = 60
        });
    }
}
