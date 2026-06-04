using Marten;
using Microsoft.AspNetCore.Mvc;
using Orion.MacroEconomics.Engine.Interfaces;
using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Controllers
{
    [ApiController]
    [Route("api/monte-carlo")]
    [Produces("application/json")]
    public class MonteCarloController(
        IMonteCarloEngine monteCarloEngine,
        IDocumentSession session) : ControllerBase
    {
        /// <summary>
        /// Runs Monte Carlo simulations over historical trade results.
        /// Returns a list of final equity values — one per simulation — starting from 100,000.
        /// Persists the full run (input trade count + final distribution) to Postgres.
        /// </summary>
        [HttpPost("run")]
        [ProducesResponseType(typeof(List<decimal>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Run([FromBody] MonteCarloRequest request, CancellationToken ct)
        {
            if (!request.Trades.Any())
                return BadRequest("Trades list cannot be empty.");

            if (request.Simulations <= 0)
                return BadRequest("Simulations count must be greater than zero.");

            var result = monteCarloEngine.Run(request.Trades, request.Simulations);

            var doc = new MonteCarloRunDocument
            {
                Simulations     = request.Simulations,
                InputTradeCount = request.Trades.Count,
                FinalEquities   = result ?? new List<decimal>()
            };
            session.Store(doc);
            await session.SaveChangesAsync(ct);

            return Ok(result);
        }

        /// <summary>Lists the most recent persisted Monte-Carlo runs.</summary>
        [HttpGet("runs")]
        [ProducesResponseType(typeof(List<MonteCarloRunDocument>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ListRunsAsync([FromQuery] int limit = 50, CancellationToken ct = default)
        {
            var runs = await session.Query<MonteCarloRunDocument>()
                .OrderByDescending(x => x.RunAt)
                .Take(Math.Clamp(limit, 1, 500))
                .ToListAsync(ct);
            return Ok(runs);
        }
    }

    public record MonteCarloRequest(List<TradeResult> Trades, int Simulations = 1000);
}
