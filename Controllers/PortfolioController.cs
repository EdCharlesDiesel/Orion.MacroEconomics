

using Microsoft.AspNetCore.Mvc;
using Orion.MacroEconomics.Engine.Interfaces;
using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Controllers
{
    [ApiController]
    [Route("api/portfolio")]
    [Produces("application/json")]
    public class PortfolioController(IPortfolioEngine portfolioEngine) : ControllerBase
    {
        /// <summary>
        /// Evaluates portfolio-level risk before accepting a new trade.
        /// Checks risk, exposure, open-trade limits, and correlation.
        /// </summary>
        [HttpPost("evaluate")]
        [ProducesResponseType(typeof(PortfolioRiskResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult Evaluate([FromBody] PortfolioEvaluateRequest request)
        {
            var result = portfolioEngine.Evaluate(
                request.NewTrade,
                request.OpenTrades,
                request.Account);
 
            return Ok(result);
        }
    }
 
    public record PortfolioEvaluateRequest(TradePlan NewTrade, List<TradePlan> OpenTrades, AccountContext Account);
}