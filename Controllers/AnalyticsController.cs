using Microsoft.AspNetCore.Mvc;
using Orion.MacroEconomics.Engine.Interfaces;
using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Controllers;

[ApiController]
[Route("api/analytics")]
[Produces("application/json")]
public sealed class AnalyticsController(
    ICorrelationEngine correlationEngine,
    IDataQualityEngine dataQualityEngine,
    ILiquidityEngine liquidityEngine,
    IHedgingEngine hedgingEngine,
    ILogger<AnalyticsController> logger) : ControllerBase
{
    /// <summary>
    /// Analyzes pair correlations based on the provided request.
    /// </summary>
    [HttpPost("correlation")]
    [ProducesResponseType(typeof(CorrelationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AnalyzeCorrelationAsync(
        [FromBody] CorrelationRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return BadRequest("Correlation request is required.");

        logger.LogInformation("Analyzing correlations");

        var result = await correlationEngine.AnalyzeAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Validates a set of OHLCV candles for data quality issues.
    /// </summary>
    [HttpPost("data-quality/validate")]
    [ProducesResponseType(typeof(DataQualityResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult ValidateCandles([FromBody] List<OhlcvBar> candles)
    {
        if (candles is null || candles.Count == 0)
            return BadRequest("At least one candle is required.");

        logger.LogInformation("Validating {Count} candles for data quality", candles.Count);

        var result = dataQualityEngine.ValidateCandles(candles);
        return Ok(result);
    }

    /// <summary>
    /// Analyzes market liquidity based on the provided request.
    /// </summary>
    [HttpPost("liquidity")]
    [ProducesResponseType(typeof(LiquidityResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AnalyzeLiquidityAsync(
        [FromBody] LiquidityRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return BadRequest("Liquidity request is required.");

        logger.LogInformation("Analyzing liquidity");

        var result = await liquidityEngine.AnalyzeAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Analyzes portfolio exposure and returns hedging recommendations.
    /// </summary>
    [HttpPost("hedging")]
    [ProducesResponseType(typeof(HedgingResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AnalyzeHedgingAsync(
        [FromBody] HedgingRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
            return BadRequest("Hedging request is required.");

        logger.LogInformation("Analyzing hedging requirements");

        var result = await hedgingEngine.AnalyzeAsync(request, cancellationToken);
        return Ok(result);
    }
}