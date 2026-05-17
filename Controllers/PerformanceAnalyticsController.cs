using Microsoft.AspNetCore.Mvc;
using Orion.MacroEconomics.Engine.Interfaces;
using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Controllers
{
    [ApiController]
    [Route("api/performance-analytics")]
    [Produces("application/json")]
    public class PerformanceAnalyticsController(IPerformanceAnalyticsEngine performanceAnalyticsEngine) : ControllerBase
    {
        /// <summary>
        /// Analyzes closed trades and calculates performance metrics:
        /// win rate, profit factor, expectancy, drawdown, and risk/reward.
        /// </summary>
        [HttpPost("analyze")]
        [ProducesResponseType(typeof(PerformanceReport), StatusCodes.Status200OK)]
        public IActionResult Analyze([FromBody] List<TradePlan>? trades)
        {
            var result = performanceAnalyticsEngine.Analyze(trades);
            return Ok(result);
        }
    }
}