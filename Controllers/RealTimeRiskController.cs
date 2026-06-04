using Marten;
using Microsoft.AspNetCore.Mvc;
using Orion.MacroEconomics.Engine.Interfaces;
using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Controllers
{
    [ApiController]
    [Route("api/real-time-risk")]
    [Produces("application/json")]
    public class RealTimeRiskController(
        IRealTimeRiskEngine realTimeRiskEngine,
        IDocumentSession session) : ControllerBase
    {
        /// <summary>
        /// Evaluates live execution risk before allowing or blocking a trade.
        /// Checks account, position, daily loss, and spread risk.
        /// Persists every evaluation to Postgres for audit.
        /// </summary>
        [HttpPost("evaluate")]
        [ProducesResponseType(typeof(RealTimeRiskResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Evaluate([FromBody] RealTimeRiskRequest request, CancellationToken ct)
        {
            var result = realTimeRiskEngine.Evaluate(
                request.Account,
                request.Execution,
                request.ExitPlan,
                request.Quote);

            var doc = new RealTimeRiskRunDocument
            {
                Pair   = result?.Pair ?? request.Execution?.Pair ?? string.Empty,
                Result = result ?? new RealTimeRiskResult()
            };
            session.Store(doc);
            await session.SaveChangesAsync(ct);

            return Ok(result);
        }

        /// <summary>Lists the most recent persisted real-time risk evaluations.</summary>
        [HttpGet("evaluations")]
        [ProducesResponseType(typeof(List<RealTimeRiskRunDocument>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ListEvaluationsAsync([FromQuery] int limit = 50, CancellationToken ct = default)
        {
            var runs = await session.Query<RealTimeRiskRunDocument>()
                .OrderByDescending(x => x.RunAt)
                .Take(Math.Clamp(limit, 1, 500))
                .ToListAsync(ct);
            return Ok(runs);
        }
    }

    public record RealTimeRiskRequest(
        AccountSnapshot Account,
        ExecutionOrder Execution,
        ExitPlan ExitPlan,
        MarketQuote Quote);
}
