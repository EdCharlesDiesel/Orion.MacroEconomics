using Microsoft.AspNetCore.Mvc;
using Orion.MacroEconomics.Engine.Interfaces;
using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Controllers;

[ApiController]
[Route("api/pricing")]
[Produces("application/json")]
public sealed class PricingController(
    IFxPricingEngine fxPricingEngine,
    ILogger<PricingController> logger) : ControllerBase
{
    /// <summary>
    /// Runs an FX price simulation from a list of macro states and initial prices.
    /// </summary>
    [HttpPost("simulate")]
    [ProducesResponseType(typeof(List<FxPrice>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult Simulate([FromBody] FxSimulationRequest request)
    {
        if (request.States is null || request.States.Count == 0)
            return BadRequest("At least one macro state is required.");

        if (request.InitialPrices is null || request.InitialPrices.Count == 0)
            return BadRequest("Initial prices are required.");

        logger.LogInformation("Running FX simulation with {Count} macro states", request.States.Count);

        var result = fxPricingEngine.Run(request.States, request.InitialPrices);
        return Ok(result);
    }

    /// <summary>
    /// Builds pricing metadata for a signal, including spread and pip value.
    /// </summary>
    [HttpPost("price")]
    [ProducesResponseType(typeof(PricingResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult Price([FromBody] PriceRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Pair))
            return BadRequest("Pair is required.");

        if (string.IsNullOrWhiteSpace(request.Direction))
            return BadRequest("Direction is required.");

        if (request.PositionSize <= 0)
            return BadRequest("PositionSize must be greater than zero.");

        logger.LogInformation("Pricing signal for {Pair} {Direction}", request.Pair, request.Direction);

        var result = fxPricingEngine.Price(request.Pair, request.Direction, request.PositionSize);
        return Ok(result);
    }
}

public sealed record FxSimulationRequest(List<MacroState> States, Dictionary<string, decimal> InitialPrices);
public sealed record PriceRequest(string Pair, string Direction, decimal PositionSize);