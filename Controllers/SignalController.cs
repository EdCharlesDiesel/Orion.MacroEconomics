using Microsoft.AspNetCore.Mvc;
using Orion.MacroEconomics.Engine.Interfaces;
using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Controllers;

[ApiController]
[Route("api/signals")]
[Produces("application/json")]
public sealed class SignalController(
    IAlphaEngine alphaEngine,
    IAlphaVantageSignalEngine alphaVantageSignalEngine,
    ILogger<SignalController> logger) : ControllerBase
{
    /// <summary>
    /// Generates an alpha signal from normalized indicators and optional macro events.
    /// </summary>
    [HttpPost("alpha/generate")]
    [ProducesResponseType(typeof(AlphaResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult GenerateAlpha([FromBody] GenerateAlphaRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Pair))
            return BadRequest("Pair is required.");

        logger.LogInformation("Generating alpha signal for {Pair}", request.Pair);

        var result = alphaEngine.Generate(request.Pair, request.Indicators, request.MacroEvents);
        return Ok(result);
    }

    /// <summary>
    /// Generates a trading signal using the AlphaVantage data provider.
    /// </summary>
    [HttpGet("alphavantage/{pair}")]
    [ProducesResponseType(typeof(TradingSignalDocument), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateAlphaVantageSignalAsync(
        [FromRoute] string pair,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(pair))
            return BadRequest("Pair is required.");

        logger.LogInformation("Generating AlphaVantage signal for {Pair}", pair);

        var result = await alphaVantageSignalEngine.GenerateSignalAsync(pair, cancellationToken);
        return Ok(result);
    }
}

public sealed record GenerateAlphaRequest(
    string Pair,
    List<NormalizedIndicator>? Indicators,
    List<MacroEvent>? MacroEvents);