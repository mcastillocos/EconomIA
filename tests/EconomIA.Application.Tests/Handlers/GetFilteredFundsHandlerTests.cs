using EconomIA.Application.DTOs;
using EconomIA.Application.Queries.GetFilteredFunds;
using EconomIA.Domain.Entities;
using EconomIA.Domain.Ports;
using EconomIA.Domain.ValueObjects;
using NSubstitute;

namespace EconomIA.Application.Tests.Handlers;

public class GetFilteredFundsHandlerTests
{
    private readonly IFundRepository _repo = Substitute.For<IFundRepository>();
    private readonly GetFilteredFundsHandler _handler;

    public GetFilteredFundsHandlerTests()
    {
        _handler = new GetFilteredFundsHandler(_repo);
    }

    private static int _isinCounter;

    private static Fund CreateFund(string name, RiskLevel risk = RiskLevel.Medium, string category = "Renta Variable",
        FundRating rating = FundRating.ThreeStars, int ranking = 1, decimal expenseRatio = 1.5m)
    {
        var counter = Interlocked.Increment(ref _isinCounter);
        var isinValue = $"IE00B{counter:D6}0";
        var fund = Fund.Create(
            new ISIN(isinValue),
            name,
            category,
            "TestGestora",
            risk,
            new Money(100m, "EUR"),
            Percentage.FromDecimal(expenseRatio));
        fund.UpdateRanking(ranking, rating);
        return fund;
    }

    [Fact]
    public async Task Handle_SinFiltros_DevuelveTodos()
    {
        var funds = new List<Fund> { CreateFund("FondoA", ranking: 1), CreateFund("FondoB", ranking: 2) };
        _repo.GetFilteredAsync(
            Arg.Any<RiskLevel?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<FundRating?>(), Arg.Any<decimal?>(), Arg.Any<decimal?>(),
            Arg.Any<decimal?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<bool>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((funds.AsReadOnly() as IReadOnlyList<Fund>, 2));

        var query = new GetFilteredFundsQuery();
        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(1, result.Page);
    }

    [Fact]
    public async Task Handle_FiltroRiesgo_PasaParametro()
    {
        _repo.GetFilteredAsync(
            riskLevel: RiskLevel.High, Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<FundRating?>(), Arg.Any<decimal?>(), Arg.Any<decimal?>(),
            Arg.Any<decimal?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<bool>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((Array.Empty<Fund>() as IReadOnlyList<Fund>, 0));

        var query = new GetFilteredFundsQuery { RiskLevel = RiskLevel.High };
        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        await _repo.Received(1).GetFilteredAsync(
            riskLevel: RiskLevel.High, Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<FundRating?>(), Arg.Any<decimal?>(), Arg.Any<decimal?>(),
            Arg.Any<decimal?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<bool>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Paginacion_CalculaPaginasCorrectamente()
    {
        var funds = new List<Fund> { CreateFund("FondoA") };
        _repo.GetFilteredAsync(
            Arg.Any<RiskLevel?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<FundRating?>(), Arg.Any<decimal?>(), Arg.Any<decimal?>(),
            Arg.Any<decimal?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<bool>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((funds.AsReadOnly() as IReadOnlyList<Fund>, 75));

        var query = new GetFilteredFundsQuery { Page = 2, PageSize = 25 };
        var result = await _handler.Handle(query, CancellationToken.None);

        Assert.Equal(75, result.TotalCount);
        Assert.Equal(2, result.Page);
        Assert.Equal(25, result.PageSize);
        Assert.Equal(3, result.TotalPages);
    }

    [Fact]
    public async Task Handle_ConPerformance_MapeoIncluido()
    {
        var fund = CreateFund("ConPerf");
        var perf = FundPerformance.Create(
            fund.Id,
            Percentage.FromDecimal(1.5m),
            Percentage.FromDecimal(4.0m),
            Percentage.FromDecimal(8.0m),
            Percentage.FromDecimal(12.0m),
            Percentage.FromDecimal(30.0m),
            Percentage.FromDecimal(50.0m),
            Percentage.FromDecimal(15.0m),
            1.2m);
        fund.AddPerformance(perf);

        _repo.GetFilteredAsync(
            Arg.Any<RiskLevel?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<FundRating?>(), Arg.Any<decimal?>(), Arg.Any<decimal?>(),
            Arg.Any<decimal?>(), Arg.Any<string?>(), Arg.Any<string?>(),
            Arg.Any<bool>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns((new List<Fund> { fund }.AsReadOnly() as IReadOnlyList<Fund>, 1));

        var result = await _handler.Handle(new GetFilteredFundsQuery(), CancellationToken.None);

        Assert.NotNull(result.Items[0].LatestPerformance);
        Assert.Equal(12.0m, result.Items[0].LatestPerformance!.Return1Year);
        Assert.Equal(1.2m, result.Items[0].LatestPerformance.SharpeRatio);
    }

    [Fact]
    public async Task Handle_FiltrosCombinados_PasaTodosLosParametros()
    {
        _repo.GetFilteredAsync(
            riskLevel: RiskLevel.Low,
            category: "Renta Fija",
            managementCompany: null,
            minRating: FundRating.FourStars,
            maxExpenseRatio: 1.0m,
            minReturn1Year: null,
            maxVolatility: null,
            searchTerm: "Amundi",
            sortBy: "Rating",
            sortDescending: true,
            page: 1,
            pageSize: 20,
            ct: Arg.Any<CancellationToken>())
            .Returns((Array.Empty<Fund>() as IReadOnlyList<Fund>, 0));

        var query = new GetFilteredFundsQuery
        {
            RiskLevel = RiskLevel.Low,
            Category = "Renta Fija",
            MinRating = FundRating.FourStars,
            MaxExpenseRatio = 1.0m,
            SearchTerm = "Amundi",
            SortBy = FundSortBy.Rating,
            SortDescending = true,
            PageSize = 20
        };

        await _handler.Handle(query, CancellationToken.None);

        await _repo.Received(1).GetFilteredAsync(
            riskLevel: RiskLevel.Low,
            category: "Renta Fija",
            managementCompany: null,
            minRating: FundRating.FourStars,
            maxExpenseRatio: 1.0m,
            minReturn1Year: null,
            maxVolatility: null,
            searchTerm: "Amundi",
            sortBy: "Rating",
            sortDescending: true,
            page: 1,
            pageSize: 20,
            ct: Arg.Any<CancellationToken>());
    }
}
