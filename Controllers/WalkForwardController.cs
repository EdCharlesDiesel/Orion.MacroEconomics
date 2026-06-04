using Marten;
using Microsoft.AspNetCore.Mvc;
using Orion.MacroEconomics.Engine.Interfaces;
using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Controllers
{
    [ApiController]
    [Route("api/walk-forward")]
    [Produces("application/json")]
    public class WalkForwardController(
        IWalkForwardEngine walkForwardEngine,
        IDocumentSession session) : ControllerBase
    {
        /// <summary>
        /// Executes walk-forward analysis using rolling train and test windows
        /// over the supplied date range. Persists the run to Postgres.
        /// </summary>
        [HttpPost("run")]
        [ProducesResponseType(typeof(List<WalkForwardResult>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Run(
            [FromBody] WalkForwardRequest request,
            CancellationToken cancellationToken)
        {
            if (request.Start >= request.End)
                return BadRequest("Start date must be earlier than end date.");

            var result = await walkForwardEngine.RunAsync(
                request.Start,
                request.End,
                cancellationToken);

            var doc = new WalkForwardRunDocument
            {
                Start        = request.Start,
                End          = request.End,
                SegmentCount = result?.Count ?? 0,
                Segments     = result ?? new List<WalkForwardResult>()
            };
            session.Store(doc);
            await session.SaveChangesAsync(cancellationToken);

            return Ok(result);
        }

        /// <summary>Lists the most recent persisted walk-forward runs.</summary>
        [HttpGet("runs")]
        [ProducesResponseType(typeof(List<WalkForwardRunDocument>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ListRunsAsync([FromQuery] int limit = 50, CancellationToken ct = default)
        {
            var runs = await session.Query<WalkForwardRunDocument>()
                .OrderByDescending(x => x.RunAt)
                .Take(Math.Clamp(limit, 1, 500))
                .ToListAsync(ct);
            return Ok(runs);
        }
    }

    public record WalkForwardRequest(DateTime Start, DateTime End);
}
