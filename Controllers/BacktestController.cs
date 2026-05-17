using Microsoft.AspNetCore.Mvc;
using Orion.MacroEconomics.Engine.Interfaces;
using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Controllers;

[ApiController]
[Route("api/backtest")]
[Produces("application/json")]
public sealed class BacktestController(
    IBacktestEngine backtestEngine,
    ILogger<BacktestController> logger) : ControllerBase
{
    /// <summary>
    /// Runs a historical backtest over a date range with a starting capital amount.
    /// </summary>
    [HttpPost("run")]
    [ProducesResponseType(typeof(List<TradeResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RunAsync([FromBody] BacktestRequest request)
    {
        if (request.Start == default || request.End == default)
            return BadRequest("Start and End dates are required.");

        if (request.Start >= request.End)
            return BadRequest("Start date must be before End date.");

        if (request.Capital <= 0)
            return BadRequest("Capital must be greater than zero.");

        logger.LogInformation(
            "Running backtest from {Start} to {End} with capital {Capital}",
            request.Start, request.End, request.Capital);

        var results = await backtestEngine.RunAsync(request.Start, request.End, request.Capital);
        return Ok(results);
    }
}

public sealed record BacktestRequest(DateTime Start, DateTime End, decimal Capital);