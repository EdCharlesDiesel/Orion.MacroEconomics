

using Microsoft.AspNetCore.Mvc;
using Orion.MacroEconomics.Engine.Interfaces;
using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Controllers
{
    [ApiController]
    [Route("api/real-backtest")]
    [Produces("application/json")]
    public class RealBacktestController(IRealBacktestEngine realBacktestEngine) : ControllerBase
    {
        /// <summary>
        /// Replays candles against portfolio positions and returns completed trade results.
        /// </summary>
        [HttpPost("run")]
        [ProducesResponseType(typeof(List<TradeResult>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Run(
            [FromBody] RealBacktestRequest request,
            CancellationToken cancellationToken)
        {
            if (!request.Candles.Any())
                return BadRequest("Candles list cannot be empty.");
 
            if (!request.Positions.Any())
                return BadRequest("Positions list cannot be empty.");
 
            var result = await realBacktestEngine.RunAsync(
                request.Candles,
                request.Positions,
                cancellationToken);
 
            return Ok(result);
        }
    }
 
    public record RealBacktestRequest(List<Candle> Candles, List<PortfolioPosition> Positions);
}