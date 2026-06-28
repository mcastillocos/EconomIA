using EconomIA.Domain.Entities;
using EconomIA.Domain.Exceptions;

namespace EconomIA.Domain.Tests.Entities;

public class CompanyTests
{
    [Fact]
    public void Create_WithValidName_ShouldCreateCompany()
    {
        var company = Company.Create("Inditex", "ITX", "ES0148396007", "BME", "España", "Retail", "Textil", "EUR");

        Assert.NotEqual(Guid.Empty, company.Id);
        Assert.Equal("Inditex", company.Name);
        Assert.Equal("ITX", company.Ticker);
        Assert.Equal("ES0148396007", company.Isin);
        Assert.Equal("BME", company.Market);
        Assert.Equal("España", company.Country);
        Assert.Equal("Retail", company.Sector);
        Assert.Equal("Textil", company.Industry);
        Assert.Equal("EUR", company.Currency);
    }

    [Fact]
    public void Create_WithEmptyName_ShouldThrowDomainException()
    {
        Assert.Throws<DomainException>(() => Company.Create(""));
    }

    [Fact]
    public void Create_WithWhitespaceName_ShouldThrowDomainException()
    {
        Assert.Throws<DomainException>(() => Company.Create("   "));
    }

    [Fact]
    public void Update_WithValidName_ShouldUpdateFields()
    {
        var company = Company.Create("Inditex");
        var originalUpdatedAt = company.UpdatedAt;

        company.Update("Inditex SA", "ITX", "ES0148396007", "BME", "España", "Retail", "Textil", "EUR", "H&M, Primark", null, "tikr", "Nota test");

        Assert.Equal("Inditex SA", company.Name);
        Assert.Equal("ITX", company.Ticker);
        Assert.Equal("H&M, Primark", company.Competitors);
        Assert.Equal("Nota test", company.Notes);
        Assert.True(company.UpdatedAt >= originalUpdatedAt);
    }

    [Fact]
    public void Update_WithEmptyName_ShouldThrowDomainException()
    {
        var company = Company.Create("Inditex");
        Assert.Throws<DomainException>(() => company.Update("", null, null, null, null, null, null, null, null, null, null, null));
    }

    [Fact]
    public void Create_SetsTimestamps()
    {
        var before = DateTime.UtcNow;
        var company = Company.Create("Test Co");
        var after = DateTime.UtcNow;

        Assert.InRange(company.CreatedAt, before, after);
        Assert.InRange(company.UpdatedAt, before, after);
    }
}
