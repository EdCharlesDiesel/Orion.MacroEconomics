using Microsoft.AspNetCore.Mvc;
using Orion.MacroEconomics.Engine.Interfaces;
using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Controllers
{
    [ApiController]
    [Route("api/model-validation")]
    [Produces("application/json")]
    public class ModelValidationController(IModelValidationEngine modelValidationEngine) : ControllerBase
    {
        /// <summary>
        /// Validates model performance using a performance report and closed trades.
        /// </summary>
        [HttpPost("validate")]
        [ProducesResponseType(typeof(ModelValidationReport), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult Validate([FromBody] ModelValidationRequest request)
        {
            if (!request.Trades.Any())
                return BadRequest("Trades list cannot be empty.");

            var result = modelValidationEngine.Validate(request.Performance, request.Trades);
            return Ok(result);
        }
    }

    public record ModelValidationRequest(PerformanceReport Performance, List<TradePlan> Trades);
}





