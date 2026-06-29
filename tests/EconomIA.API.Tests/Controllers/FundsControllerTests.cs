using System.Net;
using System.Net.Http.Json;
using EconomIA.Application.DTOs;
using EconomIA.Application.Queries.GetFilteredFunds;
using EconomIA.Domain.ValueObjects;

namespace EconomIA.API.Tests.Controllers;

public class FundsControllerTests
{
    private EconomIAWebFactory CreateFactory() => new();

    [Fact]
    public async Task GetTopFunds_SinDatos_DevuelveListaVacia()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/funds/top/10");

        response.EnsureSuccessStatusCode();
        var funds = await response.Content.ReadFromJsonAsync<List<FundDto>>();
        Assert.NotNull(funds);
        Assert.Empty(funds);
    }

    [Fact]
    public async Task GetTopFunds_ConDatos_DevuelveFondos()
    {
        await using var factory = CreateFactory();
        var fund = EconomIAWebFactory.CreateTestFund("Test Fund Alpha", ranking: 1);
        await factory.SeedFundsAsync(fund);
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/funds/top/10");

        response.EnsureSuccessStatusCode();
        var funds = await response.Content.ReadFromJsonAsync<List<FundDto>>();
        Assert.NotNull(funds);
        Assert.Contains(funds, f => f.Name == "Test Fund Alpha");
    }

    [Fact]
    public async Task GetFundDetail_Existente_Devuelve200()
    {
        await using var factory = CreateFactory();
        var fund = EconomIAWebFactory.CreateTestFund("Detail Fund");
        await factory.SeedFundsAsync(fund);
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/funds/{fund.Id}");

        response.EnsureSuccessStatusCode();
        var dto = await response.Content.ReadFromJsonAsync<FundDto>();
        Assert.NotNull(dto);
        Assert.Equal("Detail Fund", dto.Name);
    }

    [Fact]
    public async Task GetFundDetail_NoExiste_Devuelve404()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/funds/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetFundsByRisk_FiltraCorrectamente()
    {
        await using var factory = CreateFactory();
        var lowRisk = EconomIAWebFactory.CreateTestFund("Low Risk Fund", RiskLevel.Low, ranking: 1);
        var highRisk = EconomIAWebFactory.CreateTestFund("High Risk Fund", RiskLevel.High, ranking: 2);
        await factory.SeedFundsAsync(lowRisk, highRisk);
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/funds/by-risk/Low");

        response.EnsureSuccessStatusCode();
        var funds = await response.Content.ReadFromJsonAsync<List<FundDto>>();
        Assert.NotNull(funds);
        Assert.All(funds, f => Assert.Equal(RiskLevel.Low, f.RiskLevel));
    }

    [Fact]
    public async Task GetFilteredFunds_SinFiltros_DevuelvePaginado()
    {
        await using var factory = CreateFactory();
        var fund = EconomIAWebFactory.CreateTestFund("Filtered Fund", ranking: 1);
        await factory.SeedFundsAsync(fund);
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/funds/filter");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<FilteredFundsResponse>();
        Assert.NotNull(result);
        Assert.True(result.TotalCount >= 1);
        Assert.Equal(1, result.Page);
    }

    [Fact]
    public async Task GetFilteredFunds_PorCategoria_FiltraCorrectamente()
    {
        await using var factory = CreateFactory();
        var rv = EconomIAWebFactory.CreateTestFund("RV Fund", category: "Renta Variable", ranking: 1);
        var rf = EconomIAWebFactory.CreateTestFund("RF Fund", category: "Renta Fija", ranking: 2);
        await factory.SeedFundsAsync(rv, rf);
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/funds/filter?category=Renta%20Fija");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<FilteredFundsResponse>();
        Assert.NotNull(result);
        Assert.All(result.Items, f => Assert.Equal("Renta Fija", f.Category));
    }

    [Fact]
    public async Task GetFilteredFunds_Busqueda_EncuentraPorNombre()
    {
        await using var factory = CreateFactory();
        var fund = EconomIAWebFactory.CreateTestFund("Amundi Europe Growth", ranking: 1);
        await factory.SeedFundsAsync(fund);
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/funds/filter?search=amundi");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<FilteredFundsResponse>();
        Assert.NotNull(result);
        Assert.Contains(result.Items, f => f.Name.Contains("Amundi"));
    }

    [Fact]
    public async Task GetFilteredFunds_PageSizeInvalido_Normaliza()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/funds/filter?pageSize=999&page=-1");

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<FilteredFundsResponse>();
        Assert.NotNull(result);
        Assert.Equal(50, result.PageSize);
        Assert.Equal(1, result.Page);
    }

    [Fact]
    public async Task GetCategories_DevuelveLista()
    {
        await using var factory = CreateFactory();
        var fund = EconomIAWebFactory.CreateTestFund("Cat Fund", category: "Mixtos", ranking: 1);
        await factory.SeedFundsAsync(fund);
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/funds/categories");

        response.EnsureSuccessStatusCode();
        var categories = await response.Content.ReadFromJsonAsync<List<string>>();
        Assert.NotNull(categories);
        Assert.Contains("Mixtos", categories);
    }

    [Fact]
    public async Task GetManagementCompanies_DevuelveLista()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/funds/management-companies");

        response.EnsureSuccessStatusCode();
        var companies = await response.Content.ReadFromJsonAsync<List<string>>();
        Assert.NotNull(companies);
    }
}
