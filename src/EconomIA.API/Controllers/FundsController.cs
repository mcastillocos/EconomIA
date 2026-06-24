using EconomIA.Application.Commands.RefreshMarketData;
using EconomIA.Application.Commands.UpdateFundPrice;
using EconomIA.Application.DTOs;
using EconomIA.Application.Queries.GetFundDetail;
using EconomIA.Application.Queries.GetFundsByRisk;
using EconomIA.Application.Queries.GetTopFunds;
using EconomIA.Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EconomIA.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FundsController : ControllerBase
{
    private readonly IMediator _mediator;

    public FundsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("top/{count:int?}")]
    [ProducesResponseType(typeof(IReadOnlyList<FundDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTopFunds(int count = 100, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetTopFundsQuery(count), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(FundDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFundDetail(Guid id, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetFundDetailQuery(id), ct);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("by-risk/{riskLevel}")]
    [ProducesResponseType(typeof(IReadOnlyList<FundDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFundsByRisk(RiskLevel riskLevel, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetFundsByRiskQuery(riskLevel), ct);
        return Ok(result);
    }

    [HttpPost("{id:guid}/price")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePrice(Guid id, [FromBody] UpdatePriceRequest request, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new UpdateFundPriceCommand(id, request.Price, request.Currency), ct);
        return result ? NoContent() : NotFound();
    }

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    public async Task<IActionResult> RefreshMarketData(CancellationToken ct = default)
    {
        var count = await _mediator.Send(new RefreshMarketDataCommand(), ct);
        return Ok(new { FundsUpdated = count });
    }
}

public record UpdatePriceRequest(decimal Price, string Currency);
