using Microsoft.AspNetCore.Mvc;
using Orion.MacroEconomics.Engine.Interfaces;
using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Controllers
{
    [ApiController]
    [Route("api/walk-forward")]
    [Produces("application/json")]
    public class WalkForwardController(IWalkForwardEngine walkForwardEngine) : ControllerBase
    {
        /// <summary>
        /// Executes walk-forward analysis using rolling train and test windows
        /// over the supplied date range.
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
 
            return Ok(result);
        }
    }
 
    public record WalkForwardRequest(DateTime Start, DateTime End);
}