using Microsoft.AspNetCore.Mvc;
using Orion.MacroEconomics.Configuration;
using Orion.MacroEconomics.Engine.Interfaces;

namespace Orion.MacroEconomics.Controllers;

[ApiController]
[Route("api/alpha-vantage")]
public sealed class AlphaVantageSignalController(
    IAlphaVantageSignalEngine signalEngine,
    IGmailSignalNotificationService gmail,
    ILogger<AlphaVantageSignalController> logger) : ControllerBase
{
    [HttpPost("signal/{pair}")]
    public async Task<IActionResult> GenerateSignal(
        string pair,
        [FromQuery] bool sendEmail = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var signal = await signalEngine.GenerateSignalAsync(pair, cancellationToken);

            if (sendEmail && signal.Direction != "NEUTRAL")
                await gmail.SendSignalAsync(signal, cancellationToken);

            return Ok(new
            {
                signal,
                emailSent = sendEmail && signal.Direction != "NEUTRAL"
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to generate Alpha Vantage signal for {Pair}", pair);
            return BadRequest(new { error = ex.Message });
        }
    }
}