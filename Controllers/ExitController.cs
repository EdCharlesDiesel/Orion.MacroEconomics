using Microsoft.AspNetCore.Mvc;
using Orion.MacroEconomics.Engine.Interfaces;
using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Controllers;

[ApiController]
[Route("api/exit")]
[Produces("application/json")]
public sealed class ExitController(
    IExitEngine exitEngine,
    ILogger<ExitController> logger) : ControllerBase
{
    /// <summary>
    /// Determines whether an open position should exit based on the current candle.
    /// The out parameter exitPrice is wrapped in the response body.
    /// </summary>
    [HttpPost("should-exit")]
    [ProducesResponseType(typeof(ShouldExitResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult ShouldExit([FromBody] ShouldExitRequest request)
    {
        if (request.Position is null)
            return BadRequest("Position is required.");

        if (request.Candle is null)
            return BadRequest("Candle is required.");

        logger.LogInformation("Evaluating exit for position on {Pair}", request.Position.Pair);

        var shouldExit = exitEngine.ShouldExit(request.Position, request.Candle, out var exitPrice);

        return Ok(new ShouldExitResponse(shouldExit, exitPrice));
    }

    /// <summary>
    /// Calculates an exit plan including stop loss and take profit levels.
    /// </summary>
    [HttpPost("calculate")]
    [ProducesResponseType(typeof(ExitPlan), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult Calculate([FromBody] ExitPlanRequest request)
    {
        if (request.Signal is null)
            return BadRequest("Signal is required.");

        if (request.Execution is null)
            return BadRequest("Execution is required.");

        if (request.Risk is null)
            return BadRequest("Risk is required.");

        logger.LogInformation("Calculating exit plan");

        var result = exitEngine.Calculate(request.Signal, request.Execution, request.Risk, request.Normalized);
        return Ok(result);
    }
}

public sealed record ShouldExitRequest(OpenPosition Position, Candle Candle);

/// <summary>
/// Wraps the ShouldExit out parameter for HTTP transport.
/// </summary>
public sealed record ShouldExitResponse(bool ShouldExit, decimal ExitPrice);

public sealed record ExitPlanRequest(
    SignalResult Signal,
    ExecutionOrder Execution,
    RiskResult Risk,
    List<NormalizedIndicator>? Normalized);