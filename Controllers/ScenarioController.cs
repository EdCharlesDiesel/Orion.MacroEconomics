using Microsoft.AspNetCore.Mvc;
using Orion.MacroEconomics.Engine.Interfaces;
using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Controllers
{
    [ApiController]
    [Route("api/scenario")]
    [Produces("application/json")]
    public class ScenarioController(IScenarioEngine scenarioEngine) : ControllerBase
    {
        /// <summary>
        /// Runs a full macro shock scenario simulation.
        /// </summary>
        [HttpPost("run")]
        [ProducesResponseType(typeof(ScenarioResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Run(
            [FromBody] Scenario scenario,
            CancellationToken cancellationToken)
        {
            var result = await scenarioEngine.RunAsync(scenario, cancellationToken);
            return Ok(result);
        }
 
        /// <summary>
        /// Builds a simple scenario result from a normalized indicator and regime.
        /// </summary>
        [HttpPost("build")]
        [ProducesResponseType(typeof(ScenarioResult), StatusCodes.Status200OK)]
        public IActionResult Build([FromBody] ScenarioBuildRequest request)
        {
            var result = scenarioEngine.Build(request.Normalized, request.Regime);
            return Ok(result);
        }
    }
 
    public record ScenarioBuildRequest(NormalizedIndicator Normalized, RegimeResult Regime);
}