

using Microsoft.AspNetCore.Mvc;
using Orion.MacroEconomics.Engine.Interfaces;
using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Controllers
{
    [ApiController]
    [Route("api/probabilistic-scenario")]
    [Produces("application/json")]
    public class ProbabilisticScenarioController(IProbabilisticScenarioEngine probabilisticScenarioEngine)
        : ControllerBase
    {
        /// <summary>
        /// Runs multiple probabilistic scenarios asynchronously.
        /// </summary>
        [HttpPost("run")]
        [ProducesResponseType(typeof(List<SimulationResult>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Run([FromBody] List<ProbabilisticScenario>? scenarios)
        {
            if (scenarios is null || !scenarios.Any())
                return BadRequest("Scenarios list cannot be empty.");
 
            var result = await probabilisticScenarioEngine.RunAsync(scenarios);
            return Ok(result);
        }
 
        /// <summary>
        /// Calculates probability for a normalized macro scenario.
        /// </summary>
        [HttpPost("calculate")]
        [ProducesResponseType(typeof(ProbabilisticScenarioResult), StatusCodes.Status200OK)]
        public IActionResult Calculate([FromBody] ProbabilisticScenarioCalculateRequest request)
        {
            var result = probabilisticScenarioEngine.Calculate(
                request.Normalized,
                request.Regime,
                request.Scenario);
 
            return Ok(result);
        }
    }
 
    public record ProbabilisticScenarioCalculateRequest(
        NormalizedIndicator Normalized,
        RegimeResult Regime,
        ScenarioResult Scenario);
}