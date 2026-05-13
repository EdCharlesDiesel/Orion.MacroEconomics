using Microsoft.AspNetCore.Mvc;
using Orion.MacroEconomics.DTO;
using Orion.MacroEconomics.Free;
using Orion.MacroEconomics.Interfaces;

namespace Orion.MacroEconomics.Controllers
{
    [ApiController]
    [Route("api/regime")]
    public sealed class RegimeController(
        RegimeEngineFree regimeEngine,
        IRegimeDataProvider regimeDataProvider,
        ILogger<RegimeController> logger) : ControllerBase
    {
        [HttpPost("analyze")]
        public ActionResult<RegimeResult> Analyze([FromBody] RegimeInput input)
        {
            try
            {
                var result = regimeEngine.Analyze(input);
                return Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error analyzing regime");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("auto")]
        public async Task<ActionResult<RegimeResult>> AnalyzeAutomatically(
            CancellationToken cancellationToken = default)
        {
            try
            {
                var input = await regimeDataProvider.GetAsync(cancellationToken);
                var result = regimeEngine.Analyze(input);

                return Ok(result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error running automatic regime analysis");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("input")]
        public async Task<ActionResult<RegimeInput>> GetRegimeInput(
            CancellationToken cancellationToken = default)
        {
            try
            {
                var input = await regimeDataProvider.GetAsync(cancellationToken);
                return Ok(input);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting regime input");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}