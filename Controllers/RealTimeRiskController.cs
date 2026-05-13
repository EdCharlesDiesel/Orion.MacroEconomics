using Microsoft.AspNetCore.Mvc;
using Orion.MacroEconomics.Engine.Interfaces;
using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Controllers
{
    [ApiController]
    [Route("api/real-time-risk")]
    [Produces("application/json")]
    public class RealTimeRiskController(IRealTimeRiskEngine realTimeRiskEngine) : ControllerBase
    {
        /// <summary>
        /// Evaluates live execution risk before allowing or blocking a trade.
        /// Checks account, position, daily loss, and spread risk.
        /// </summary>
        [HttpPost("evaluate")]
        [ProducesResponseType(typeof(RealTimeRiskResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult Evaluate([FromBody] RealTimeRiskRequest request)
        {
            var result = realTimeRiskEngine.Evaluate(
                request.Account,
                request.Execution,
                request.ExitPlan,
                request.Quote);
 
            return Ok(result);
        }
    }
 
    public record RealTimeRiskRequest(
        AccountSnapshot Account,
        ExecutionOrder Execution,
        ExitPlan ExitPlan,
        MarketQuote Quote);
}