using Microsoft.AspNetCore.Mvc;
using Orion.MacroEconomics.Engine.Interfaces;
using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Controllers;

[ApiController]
[Route("api/alerts")]
[Produces("application/json")]
public sealed class AlertController(
    IAlertEngine alertEngine,
    ILogger<AlertController> logger) : ControllerBase
{
    /// <summary>
    /// Evaluates a trading decision and returns any triggered alerts.
    /// </summary>
    [HttpPost("evaluate")]
    [ProducesResponseType(typeof(List<TradingAlert>), StatusCodes.Status200OK)]
    public IActionResult Evaluate([FromBody] TradingDecision? decision)
    {
        logger.LogInformation("Evaluating alerts for trading decision");

        var alerts = alertEngine.Evaluate(decision);
        return Ok(alerts);
    }
}