using Microsoft.AspNetCore.Mvc;
using Orion.MacroEconomics.Engine.Interfaces;
using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Controllers
{
    [ApiController]
    [Route("api/monte-carlo")]
    [Produces("application/json")]
    public class MonteCarloController(IMonteCarloEngine monteCarloEngine) : ControllerBase
    {
        /// <summary>
        /// Runs Monte Carlo simulations over historical trade results.
        /// Returns a list of final equity values — one per simulation — starting from 100,000.
        /// </summary>
        [HttpPost("run")]
        [ProducesResponseType(typeof(List<decimal>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult Run([FromBody] MonteCarloRequest request)
        {
            if (!request.Trades.Any())
                return BadRequest("Trades list cannot be empty.");

            if (request.Simulations <= 0)
                return BadRequest("Simulations count must be greater than zero.");

            var result = monteCarloEngine.Run(request.Trades, request.Simulations);
            return Ok(result);
        }
    }

    public record MonteCarloRequest(List<TradeResult> Trades, int Simulations = 1000);
}