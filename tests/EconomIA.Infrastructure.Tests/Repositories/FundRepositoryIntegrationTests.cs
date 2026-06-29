using EconomIA.Domain.Entities;
using EconomIA.Domain.ValueObjects;
using EconomIA.Infrastructure.Persistence;
using EconomIA.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Testcontainers.MsSql;

namespace EconomIA.Infrastructure.Tests.Repositories;

[Trait("Category", "Integration")]
public class FundRepositoryIntegrationTests : IAsyncLifetime
{
    private readonly MsSqlContainer _sqlContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    private EconomIADbContext _dbContext = null!;
    private FundRepository _repo = null!;

    public async Task InitializeAsync()
    {
        await _sqlContainer.StartAsync();

        var options = new DbContextOptionsBuilder<EconomIADbContext>()
            .UseSqlServer(_sqlContainer.GetConnectionString())
            .Options;

        _dbContext = new EconomIADbContext(options);
        await _dbContext.Database.EnsureCreatedAsync();
        _repo = new FundRepository(_dbContext);
    }

    public async Task DisposeAsync()
    {
        await _dbContext.DisposeAsync();
        await _sqlContainer.DisposeAsync();
    }

    private static Fund CreateFund(string name, RiskLevel risk = RiskLevel.Medium,
        string category = "Renta Variable", int ranking = 1, decimal ter = 1.5m)
    {
        var fund = Fund.Create(
            new ISIN($"IE00B{Guid.NewGuid().ToString()[..6].ToUpper()}0"),
            name, category, "Gestora Test", risk,
            new Money(100m, "EUR"), Percentage.FromDecimal(ter));
        fund.UpdateRanking(ranking, FundRating.ThreeStars);
        return fund;
    }

    [Fact]
    public async Task AddAsync_YGetById_PersisteFondo()
    {
        var fund = CreateFund("Fondo Persistente");
        await _repo.AddAsync(fund);
        await _repo.SaveChangesAsync();

        var retrieved = await _repo.GetByIdAsync(fund.Id);

        Assert.NotNull(retrieved);
        Assert.Equal("Fondo Persistente", retrieved.Name);
        Assert.Equal(fund.Isin, retrieved.Isin);
    }

    [Fact]
    public async Task GetByIsinAsync_Encuentra()
    {
        var fund = CreateFund("ISIN Test");
        await _repo.AddAsync(fund);
        await _repo.SaveChangesAsync();

        var found = await _repo.GetByIsinAsync(fund.Isin);

        Assert.NotNull(found);
        Assert.Equal(fund.Id, found.Id);
    }

    [Fact]
    public async Task GetTopFundsAsync_OrdenadoPorRanking()
    {
        var fund3 = CreateFund("Third", ranking: 3);
        var fund1 = CreateFund("First", ranking: 1);
        var fund2 = CreateFund("Second", ranking: 2);
        await _repo.AddAsync(fund3);
        await _repo.AddAsync(fund1);
        await _repo.AddAsync(fund2);
        await _repo.SaveChangesAsync();

        var top = await _repo.GetTopFundsAsync(3);

        Assert.Equal(3, top.Count);
        Assert.Equal("First", top[0].Name);
        Assert.Equal("Second", top[1].Name);
        Assert.Equal("Third", top[2].Name);
    }

    [Fact]
    public async Task GetByRiskLevelAsync_FiltraPorRiesgo()
    {
        var low = CreateFund("Low Fund", RiskLevel.Low, ranking: 1);
        var high = CreateFund("High Fund", RiskLevel.High, ranking: 2);
        await _repo.AddAsync(low);
        await _repo.AddAsync(high);
        await _repo.SaveChangesAsync();

        var lowFunds = await _repo.GetByRiskLevelAsync(RiskLevel.Low);

        Assert.Single(lowFunds);
        Assert.Equal("Low Fund", lowFunds[0].Name);
    }

    [Fact]
    public async Task GetFilteredAsync_MultiFiltro_Funciona()
    {
        var rv = CreateFund("RV Alpha", RiskLevel.Medium, "Renta Variable", ranking: 1, ter: 0.5m);
        var rf = CreateFund("RF Beta", RiskLevel.Low, "Renta Fija", ranking: 2, ter: 2.0m);
        var rvCaro = CreateFund("RV Caro", RiskLevel.Medium, "Renta Variable", ranking: 3, ter: 3.0m);
        await _repo.AddAsync(rv);
        await _repo.AddAsync(rf);
        await _repo.AddAsync(rvCaro);
        await _repo.SaveChangesAsync();

        var (funds, total) = await _repo.GetFilteredAsync(
            category: "Renta Variable",
            maxExpenseRatio: 1.0m);

        Assert.Equal(1, total);
        Assert.Single(funds);
        Assert.Equal("RV Alpha", funds[0].Name);
    }

    [Fact]
    public async Task GetFilteredAsync_Busqueda_PorNombre()
    {
        var fund = CreateFund("Amundi World Growth");
        await _repo.AddAsync(fund);
        await _repo.SaveChangesAsync();

        var (funds, _) = await _repo.GetFilteredAsync(searchTerm: "amundi");

        Assert.Single(funds);
        Assert.Contains("Amundi", funds[0].Name);
    }

    [Fact]
    public async Task GetFilteredAsync_Paginacion()
    {
        for (int i = 1; i <= 10; i++)
            await _repo.AddAsync(CreateFund($"Fund {i:D2}", ranking: i));
        await _repo.SaveChangesAsync();

        var (page1, total) = await _repo.GetFilteredAsync(page: 1, pageSize: 3);
        var (page2, _) = await _repo.GetFilteredAsync(page: 2, pageSize: 3);

        Assert.Equal(10, total);
        Assert.Equal(3, page1.Count);
        Assert.Equal(3, page2.Count);
        Assert.NotEqual(page1[0].Name, page2[0].Name);
    }

    [Fact]
    public async Task GetDistinctCategoriesAsync_DevuelveSinDuplicados()
    {
        await _repo.AddAsync(CreateFund("A", category: "Renta Variable", ranking: 1));
        await _repo.AddAsync(CreateFund("B", category: "Renta Variable", ranking: 2));
        await _repo.AddAsync(CreateFund("C", category: "Renta Fija", ranking: 3));
        await _repo.SaveChangesAsync();

        var categories = await _repo.GetDistinctCategoriesAsync();

        Assert.Equal(2, categories.Count);
        Assert.Contains("Renta Variable", categories);
        Assert.Contains("Renta Fija", categories);
    }

    [Fact]
    public async Task UpdateAsync_ActualizaPrecio()
    {
        var fund = CreateFund("Updatable");
        await _repo.AddAsync(fund);
        await _repo.SaveChangesAsync();

        fund.UpdatePrice(new Money(150m, "EUR"));
        await _repo.UpdateAsync(fund);
        await _repo.SaveChangesAsync();

        var updated = await _repo.GetByIdAsync(fund.Id);
        Assert.Equal(150m, updated!.NetAssetValue.Amount);
    }
}
