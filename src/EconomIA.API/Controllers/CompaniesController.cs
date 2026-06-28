using EconomIA.Application.DTOs;
using EconomIA.Domain.Entities;
using EconomIA.Domain.Ports;
using Microsoft.AspNetCore.Mvc;

namespace EconomIA.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CompaniesController : ControllerBase
{
    private readonly ICompanyRepository _repository;

    public CompaniesController(ICompanyRepository repository)
    {
        _repository = repository;
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
}
