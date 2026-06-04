using Marten;
using Microsoft.AspNetCore.Mvc;
using Orion.MacroEconomics.Engine.Interfaces;
using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Controllers;

[ApiController]
[Route("api/backtest")]
[Produces("application/json")]
public sealed class BacktestController(
    IBacktestEngine backtestEngine,
    IDocumentSession session,
    ILogger<BacktestController> logger) : ControllerBase
{
    /// <summary>
    /// Runs a historical backtest over a date range with a starting capital amount.
    /// Persists the full run (request params + trades) to Postgres for audit/replay.
    /// </summary>
    [HttpPost("run")]
    [ProducesResponseType(typeof(List<TradeResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RunAsync([FromBody] BacktestRequest request, CancellationToken ct)
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

        var doc = new BacktestRunDocument
        {
            Start      = request.Start,
            End        = request.End,
            Capital    = request.Capital,
            TradeCount = results?.Count ?? 0,
            Trades     = results ?? new List<TradeResult>()
        };
        session.Store(doc);
        await session.SaveChangesAsync(ct);

        return Ok(results);
    }

    /// <summary>Lists the most recent persisted backtest runs.</summary>
    [HttpGet("runs")]
    [ProducesResponseType(typeof(List<BacktestRunDocument>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListRunsAsync([FromQuery] int limit = 50, CancellationToken ct = default)
    {
        var runs = await session.Query<BacktestRunDocument>()
            .OrderByDescending(x => x.RunAt)
            .Take(Math.Clamp(limit, 1, 500))
            .ToListAsync(ct);
        return Ok(runs);
    }
}

public sealed record BacktestRequest(DateTime Start, DateTime End, decimal Capital);
