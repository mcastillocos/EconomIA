using EconomIA.Application.DTOs;
using EconomIA.Infrastructure.ExternalServices;
using Microsoft.AspNetCore.Mvc;

namespace EconomIA.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PortfolioOptimizerController : ControllerBase
{
    private readonly PortfolioOptimizerService _optimizer;

    public PortfolioOptimizerController(PortfolioOptimizerService optimizer) => _optimizer = optimizer;

    [HttpPost("optimize")]
    public async Task<IActionResult> Optimize([FromBody] PortfolioOptimizationRequest request)
    {
        try
        {
            var result = await _optimizer.OptimizeAsync(request);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
