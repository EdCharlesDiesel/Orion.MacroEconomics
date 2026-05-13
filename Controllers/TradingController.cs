using Microsoft.AspNetCore.Mvc;
using Orion.MacroEconomics.Engine.Interfaces;
using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Controllers;

[ApiController]
[Route("api/trading")]
[Produces("application/json")]
public sealed class TradingController(
    ILiveTradingOrchestrator orchestrator,
    ILogger<TradingController> logger) : ControllerBase
{
    /// <summary>
    /// Executes the full live trading pipeline: data ingestion → signal → risk → decision.
    /// Returns the final trading decision result.
    /// </summary>
    [HttpPost("run")]
    [ProducesResponseType(typeof(LiveTradingResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult Run([FromBody] LiveTradingRequest request)
    {
        if (request.Input is null)
            return BadRequest("ForexMarketInput is required.");

        if (request.Account is null)
            return BadRequest("AccountContext is required.");

        if (request.OrderBook is null)
            return BadRequest("OrderBook is required.");

        logger.LogInformation(
            "Running live trading pipeline for {Pair}",
            request.Input.Pair);

        var result = orchestrator.Run(request.Input, request.Account, request.OrderBook);
        return Ok(result);
    }
}

public sealed record LiveTradingRequest(
    ForexMarketInput Input,
    AccountContext Account,
    OrderBook OrderBook);