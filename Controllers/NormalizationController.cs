
using Microsoft.AspNetCore.Mvc;
using Orion.MacroEconomics.Engine.Interfaces.Orion.API.TradingEconomics.Engine.Interfaces;
using Orion.MacroEconomics.Entities;
namespace Orion.MacroEconomics.Controllers
{

    [ApiController]
    [Route("api/normalization")]
    [Produces("application/json")]
    public class NormalizationController : ControllerBase
    {
        private readonly INormalizationEngine _normalizationEngine;

        public NormalizationController(INormalizationEngine normalizationEngine)
        {
            _normalizationEngine = normalizationEngine;
        }

        /// <summary>
        /// Normalizes a collection of raw economic indicators.
        /// </summary>
        [HttpPost("normalize")]
        [ProducesResponseType(typeof(List<NormalizedIndicator>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult Normalize([FromBody] List<EconomicIndicator>? indicators)
        {
            if (indicators is null || !indicators.Any())
                return BadRequest("Indicators list cannot be empty.");

            var result = _normalizationEngine.Normalize(indicators);
            return Ok(result);
        }
    }
}