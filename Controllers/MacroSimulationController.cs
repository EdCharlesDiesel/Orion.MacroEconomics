using Marten;
using Microsoft.AspNetCore.Mvc;
using Orion.MacroEconomics.DTO;
using Orion.MacroEconomics.Engine.Interfaces;
using Orion.MacroEconomics.Entities;
using Orion.MacroEconomics.Models;

namespace Orion.MacroEconomics.Controllers
{
    [ApiController]
    [Route("api/macro-simulation")]
    [Produces("application/json")]
    public class MacroSimulationController(
        IMacroSimulationEngine macroSimulationEngine,
        IDocumentSession session) : ControllerBase
    {
        /// <summary>
        /// Runs a macro state simulation for a specified number of steps.
        /// Persists the resulting trajectory to Postgres.
        /// </summary>
        [HttpPost("run")]
        [ProducesResponseType(typeof(List<MacroState>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Run([FromBody] MacroSimulationRunRequest request, CancellationToken ct)
        {
            if (request.Steps <= 0)
                return BadRequest("Steps must be greater than zero.");

            var result = macroSimulationEngine.Run(request.Initial, request.Steps);

            session.Store(new MacroSimulationRunDocument
            {
                Source = "Run",
                Result = new MacroSimulationResult
                {
                    Direction    = "TRAJECTORY",
                    Confidence   = 0m,
                    States       = result ?? new List<MacroState>(),
                    SuccessRate  = 0m,
                    TimestampUtc = DateTime.UtcNow
                }
            });
            await session.SaveChangesAsync(ct);

            return Ok(result);
        }

        /// <summary>
        /// Simulates macro outcomes based on normalized indicators, regime, and probabilities.
        /// Persists the result to Postgres.
        /// </summary>
        [HttpPost("simulate")]
        [ProducesResponseType(typeof(MacroSimulationResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Simulate([FromBody] MacroSimulationRequest request, CancellationToken ct)
        {
            var result = macroSimulationEngine.Simulate(
                request.Normalized,
                request.Regime,
                request.Probabilities);

            session.Store(new MacroSimulationRunDocument
            {
                Source = "Simulate",
                Result = result ?? new MacroSimulationResult()
            });
            await session.SaveChangesAsync(ct);

            return Ok(result);
        }

        /// <summary>Lists the most recent persisted macro-simulation runs.</summary>
        [HttpGet("runs")]
        [ProducesResponseType(typeof(List<MacroSimulationRunDocument>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ListRunsAsync([FromQuery] int limit = 50, CancellationToken ct = default)
        {
            var runs = await session.Query<MacroSimulationRunDocument>()
                .OrderByDescending(x => x.RunAt)
                .Take(Math.Clamp(limit, 1, 500))
                .ToListAsync(ct);
            return Ok(runs);
        }
    }

    public record MacroSimulationRunRequest(MacroState Initial, int Steps);
    public record MacroSimulationRequest(NormalizedIndicator Normalized, RegimeResult Regime, ProbabilisticScenarioResult Probabilities);
}
