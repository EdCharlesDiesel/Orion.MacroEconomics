using Marten;
using Microsoft.AspNetCore.Mvc;
using Orion.MacroEconomics.Engine.Interfaces;
using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Controllers
{
    [ApiController]
    [Route("api/performance-analytics")]
    [Produces("application/json")]
    public class PerformanceAnalyticsController(
        IPerformanceAnalyticsEngine performanceAnalyticsEngine,
        IDocumentSession session) : ControllerBase
    {
        /// <summary>
        /// Analyzes closed trades and calculates performance metrics:
        /// win rate, profit factor, expectancy, drawdown, and risk/reward.
        /// Persists the computed report to Postgres for history.
        /// </summary>
        [HttpPost("analyze")]
        [ProducesResponseType(typeof(PerformanceReport), StatusCodes.Status200OK)]
        public async Task<IActionResult> Analyze([FromBody] List<TradePlan>? trades, CancellationToken ct)
        {
            var result = performanceAnalyticsEngine.Analyze(trades);

            var doc = new PerformanceReportDocument
            {
                InputTrades = trades?.Count ?? 0,
                Report      = result ?? new PerformanceReport()
            };
            session.Store(doc);
            await session.SaveChangesAsync(ct);

            return Ok(result);
        }

        /// <summary>Lists the most recent persisted performance reports.</summary>
        [HttpGet("reports")]
        [ProducesResponseType(typeof(List<PerformanceReportDocument>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ListReportsAsync([FromQuery] int limit = 50, CancellationToken ct = default)
        {
            var runs = await session.Query<PerformanceReportDocument>()
                .OrderByDescending(x => x.RunAt)
                .Take(Math.Clamp(limit, 1, 500))
                .ToListAsync(ct);
            return Ok(runs);
        }
    }
}
