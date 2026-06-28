using EconomIA.Domain.Entities;
using EconomIA.Domain.Ports;
using EconomIA.Infrastructure.ExternalServices;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;
using Xunit;

namespace EconomIA.Infrastructure.Tests.ExternalServices;

public class YahooFinanceProviderTests
{
    private static YahooFinanceProvider CreateProvider(HttpMessageHandler handler)
    {
        var client = new HttpClient(handler) { BaseAddress = new Uri("https://query1.finance.yahoo.com") };
        var logger = new Mock<ILogger<YahooFinanceProvider>>();
        return new YahooFinanceProvider(client, logger.Object);
    }

    private static Mock<HttpMessageHandler> MockHandler(HttpStatusCode status, string content)
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = status,
                Content = new StringContent(content),
            });
        return handler;
    }

    [Fact]
    public async Task FetchTopFundsAsync_ValidResponse_ReturnsFunds()
    {
        var json = JsonSerializer.Serialize(new
        {
            quoteResponse = new
            {
                result = new[]
                {
                    new
                    {
                        symbol = "IWDA.AS",
                        regularMarketPrice = 85.5m,
                        currency = "EUR",
                        fiftyTwoWeekHigh = 90.0m,
                        fiftyTwoWeekLow = 70.0m,
                    }
                }
            }
        });

        var handler = MockHandler(HttpStatusCode.OK, json);
        var provider = CreateProvider(handler.Object);

        var funds = await provider.FetchTopFundsAsync(5);

        Assert.NotEmpty(funds);
        Assert.Equal("iShares Core MSCI World", funds[0].Name);
        Assert.Equal("IE00B4L5Y983", funds[0].Isin.Value);
    }

    [Fact]
    public async Task FetchTopFundsAsync_ApiError_ReturnsEmpty()
    {
        var handler = MockHandler(HttpStatusCode.InternalServerError, "error");
        var provider = CreateProvider(handler.Object);

        var funds = await provider.FetchTopFundsAsync(10);

        Assert.Empty(funds);
    }

    [Fact]
    public async Task FetchFundByIsinAsync_KnownIsin_ReturnsFund()
    {
        var json = JsonSerializer.Serialize(new
        {
            quoteResponse = new
            {
                result = new[]
                {
                    new
                    {
                        symbol = "CSPX.L",
                        regularMarketPrice = 520.0m,
                        currency = "USD",
                        fiftyTwoWeekHigh = 550.0m,
                        fiftyTwoWeekLow = 450.0m,
                    }
                }
            }
        });

        var handler = MockHandler(HttpStatusCode.OK, json);
        var provider = CreateProvider(handler.Object);

        var fund = await provider.FetchFundByIsinAsync("IE00B5BMR087");

        Assert.NotNull(fund);
        Assert.Equal("iShares Core S&P 500", fund.Name);
    }

    [Fact]
    public async Task FetchFundByIsinAsync_UnknownIsin_ReturnsNull()
    {
        var handler = MockHandler(HttpStatusCode.OK, "{}");
        var provider = CreateProvider(handler.Object);

        var fund = await provider.FetchFundByIsinAsync("XX0000000000");

        Assert.Null(fund);
    }

    [Fact]
    public async Task FetchPerformanceAsync_ValidChart_ReturnsPerformance()
    {
        // 13 monthly prices (1 year + current)
        var prices = Enumerable.Range(0, 13).Select(i => 100m + i * 2).ToArray();
        var json = JsonSerializer.Serialize(new
        {
            chart = new
            {
                result = new[]
                {
                    new
                    {
                        indicators = new
                        {
                            adjclose = new[]
                            {
                                new { adjclose = prices }
                            }
                        }
                    }
                }
            }
        });

        var handler = MockHandler(HttpStatusCode.OK, json);
        var provider = CreateProvider(handler.Object);

        var fundId = Guid.NewGuid();
        var perf = await provider.FetchPerformanceAsync(fundId, "IE00B4L5Y983");

        Assert.NotNull(perf);
        Assert.Equal(fundId, perf.FundId);
        Assert.True(perf.Return1Year.Value > 0); // Prices are going up
    }

    [Fact]
    public async Task FetchPerformanceAsync_UnknownIsin_ReturnsNull()
    {
        var handler = MockHandler(HttpStatusCode.OK, "{}");
        var provider = CreateProvider(handler.Object);

        var perf = await provider.FetchPerformanceAsync(Guid.NewGuid(), "UNKNOWN");

        Assert.Null(perf);
    }

    [Fact]
    public void ImplementsIMarketDataProvider()
    {
        var handler = MockHandler(HttpStatusCode.OK, "{}");
        var provider = CreateProvider(handler.Object);

        Assert.IsAssignableFrom<IMarketDataProvider>(provider);
    }
}
