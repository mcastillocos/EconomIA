using EconomIA.Domain.Entities;
using EconomIA.Domain.Exceptions;

namespace EconomIA.Domain.Tests.Entities;

public class FinancialMetricTests
{
    [Fact]
    public void Create_WithValidData_ShouldCreateMetric()
    {
        var metric = FinancialMetric.Create(
            entityType: "company",
            entityId: Guid.NewGuid(),
            metricName: "Revenue",
            value: 125_000_000m,
            ticker: "ITX",
            year: 2024,
            quarter: 4,
            currency: "EUR",
            source: "annual_report_2024.pdf",
            sourceType: "pdf",
            fileName: "annual_report_2024.pdf",
            page: "42",
            confidence: "high");

        Assert.NotEqual(Guid.Empty, metric.Id);
        Assert.Equal("Revenue", metric.MetricName);
        Assert.Equal(125_000_000m, metric.Value);
        Assert.Equal("ITX", metric.Ticker);
        Assert.Equal(2024, metric.Year);
        Assert.Equal(4, metric.Quarter);
        Assert.Equal("EUR", metric.Currency);
        Assert.Equal("high", metric.Confidence);
        Assert.False(metric.Validated);
        Assert.Null(metric.ValidatedAt);
    }

    [Fact]
    public void Create_WithEmptyMetricName_ShouldThrowDomainException()
    {
        Assert.Throws<DomainException>(() => FinancialMetric.Create("company", Guid.NewGuid(), "", 100m));
    }

    [Fact]
    public void MarkValidated_ShouldSetValidatedAndTimestamp()
    {
        var metric = FinancialMetric.Create("company", Guid.NewGuid(), "Revenue", 100m);

        metric.MarkValidated();

        Assert.True(metric.Validated);
        Assert.NotNull(metric.ValidatedAt);
    }

    [Fact]
    public void MarkUnvalidated_ShouldClearValidation()
    {
        var metric = FinancialMetric.Create("company", Guid.NewGuid(), "Revenue", 100m);
        metric.MarkValidated();

        metric.MarkUnvalidated();

        Assert.False(metric.Validated);
        Assert.Null(metric.ValidatedAt);
    }
}
