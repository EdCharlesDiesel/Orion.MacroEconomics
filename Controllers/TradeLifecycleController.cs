using Microsoft.AspNetCore.Mvc;
using Orion.MacroEconomics.Engine.Interfaces;
using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Controllers
{
    [ApiController]
    [Route("api/trade-lifecycle")]
    [Produces("application/json")]
    public class TradeLifecycleController(ITradeLifecycleEngine tradeLifecycleEngine) : ControllerBase
    {
        /// <summary>
        /// Creates an open trade plan from approved signal, risk, size, execution, and exit data.
        /// </summary>
        [HttpPost("create-plan")]
        [ProducesResponseType(typeof(TradePlan), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult CreatePlan([FromBody] CreateTradePlanRequest request)
        {
            var result = tradeLifecycleEngine.CreatePlan(
                request.Signal,
                request.Risk,
                request.Size,
                request.Execution,
                request.Exit);
 
            return StatusCode(StatusCodes.Status201Created, result);
        }
 
        /// <summary>
        /// Updates an open trade and closes it when stop loss or take profit is hit.
        /// </summary>
        [HttpPut("update")]
        [ProducesResponseType(typeof(TradePlan), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult Update([FromBody] UpdateTradePlanRequest request)
        {
            var result = tradeLifecycleEngine.Update(request.Trade, request.LatestCandle);
            return Ok(result);
        }
    }
 
    public record CreateTradePlanRequest(
        SignalResult Signal,
        RiskResult Risk,
        PositionSizeResult Size,
        ExecutionOrder Execution,
        ExitPlan Exit);
 
    public record UpdateTradePlanRequest(TradePlan Trade, OhlcvBar LatestCandle);
}