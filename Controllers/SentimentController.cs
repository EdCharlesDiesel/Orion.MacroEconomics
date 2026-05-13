using Microsoft.AspNetCore.Mvc;
using Orion.MacroEconomics.Engine.Interfaces;
using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Controllers
{
    [ApiController]
    [Route("api/sentiment")]
    [Produces("application/json")]
    public class SentimentController(ISentimentEngine sentimentEngine) : ControllerBase
    {
        /// <summary>
        /// Analyzes sentiment items and produces a directional sentiment result
        /// including weighted score, bias, confidence, and reasons.
        /// </summary>
        [HttpPost("analyze")]
        [ProducesResponseType(typeof(SentimentResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Analyze(
            [FromBody] SentimentRequest request,
            CancellationToken cancellationToken)
        {
            var result = await sentimentEngine.AnalyzeAsync(request, cancellationToken);
            return Ok(result);
        }
    }
}