using Marten;
using Microsoft.AspNetCore.Mvc;
using Orion.MacroEconomics.Engine.Interfaces;
using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Controllers
{
    [ApiController]
    [Route("api/live-trading")]
    public sealed class LiveTradingController : ControllerBase
    {
        private readonly ILiveTradingOrchestrator _orchestrator;
        private readonly IDocumentSession         _session;

        public LiveTradingController(
            ILiveTradingOrchestrator orchestrator,
            IDocumentSession session)
        {
            _orchestrator = orchestrator;
            _session      = session;
        }

        /// <summary>
        /// Runs the full live-trading pipeline (data quality → regime → signal → risk → execution → exit → trade)
        /// and persists the outcome to Postgres.
        /// </summary>
        [HttpPost("run")]
        public async Task<ActionResult<LiveTradingResult>> Run([FromBody] LiveTradingRequest request, CancellationToken ct)
        {
            var result = _orchestrator.Run(
                request.MarketInput,
                request.Account,
                request.OrderBook);

            _session.Store(new LiveTradingRunDocument
            {
                Pair   = result?.Pair ?? request.MarketInput?.Pair ?? string.Empty,
                Result = result ?? new LiveTradingResult()
            });
            await _session.SaveChangesAsync(ct);

            return Ok(result);
        }

        /// <summary>Lists the most recent persisted live-trading orchestration outcomes.</summary>
        [HttpGet("runs")]
        public async Task<ActionResult<List<LiveTradingRunDocument>>> ListRunsAsync([FromQuery] int limit = 50, CancellationToken ct = default)
        {
            var runs = await _session.Query<LiveTradingRunDocument>()
                .OrderByDescending(x => x.RunAt)
                .Take(Math.Clamp(limit, 1, 500))
                .ToListAsync(ct);
            return Ok(runs);
        }
    }
}
