using Microsoft.AspNetCore.Mvc;
using Orion.MacroEconomics.Engine.Interfaces;
using Orion.MacroEconomics.Entities;
namespace Orion.MacroEconomics.Controllers
{
    [ApiController]
    [Route("api/macro-simulation")]
    [Produces("application/json")]
    public class MacroSimulationController(IMacroSimulationEngine macroSimulationEngine) : ControllerBase
    {
        /// <summary>
        /// Runs a macro state simulation for a specified number of steps.
        /// </summary>
        [HttpPost("run")]
        [ProducesResponseType(typeof(List<MacroState>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult Run([FromBody] MacroSimulationRunRequest request)
        {
            if (request.Steps <= 0)
                return BadRequest("Steps must be greater than zero.");

            var result = macroSimulationEngine.Run(request.Initial, request.Steps);
            return Ok(result);
        }

        /// <summary>
        /// Simulates macro outcomes based on normalized indicators, regime, and probabilities.
        /// </summary>
        [HttpPost("simulate")]
        [ProducesResponseType(typeof(MacroSimulationResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult Simulate([FromBody] MacroSimulationRequest request)
        {
            var result = macroSimulationEngine.Simulate(
                request.Normalized,
                request.Regime,
                request.Probabilities);

            return Ok(result);
        }
    }

    public record MacroSimulationRunRequest(MacroState Initial, int Steps);
    public record MacroSimulationRequest(NormalizedIndicator Normalized, RegimeResult Regime, ProbabilisticScenarioResult Probabilities);
}