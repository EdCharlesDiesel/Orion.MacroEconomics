

using Microsoft.AspNetCore.Mvc;
using Orion.MacroEconomics.Engine.Interfaces;
using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Controllers
{
    [ApiController]
    [Route("api/position-sizing")]
    [Produces("application/json")]
    public class PositionSizingController(IPositionSizingEngine positionSizingEngine) : ControllerBase
    {
        /// <summary>
        /// Calculates a risk-adjusted position size using ATR-based stop distance.
        /// </summary>
        [HttpPost("calculate")]
        [ProducesResponseType(typeof(PositionSizeResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult Calculate([FromBody] PositionSizingRequest request)
        {
            var result = positionSizingEngine.Calculate(
                request.Signal,
                request.Risk,
                request.Market,
                request.Account);
 
            return Ok(result);
        }
    }
 
    public record PositionSizingRequest(
        SignalResult Signal,
        RiskResult Risk,
        NormalizedMarketContext Market,
        AccountContext Account);
}