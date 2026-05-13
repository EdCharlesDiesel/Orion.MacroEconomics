using Microsoft.AspNetCore.Mvc;
using Orion.MacroEconomics.Engine.Interfaces;
using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Controllers
{
    [ApiController]
    [Route("api/order-management")]
    [Produces("application/json")]
    public class OrderManagementController(IOrderManagementEngine orderManagementEngine) : ControllerBase
    {
        /// <summary>
        /// Creates an order request from an open trade plan.
        /// </summary>
        [HttpPost("create")]
        [ProducesResponseType(typeof(OrderRequest), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult CreateOrder([FromBody] CreateOrderRequest request)
        {
            var result = orderManagementEngine.CreateOrder(
                request.Trade,
                request.Size,
                request.Account);
 
            return Ok(result);
        }
 
        /// <summary>
        /// Validates an execution fill against the original order.
        /// </summary>
        [HttpPost("validate-fill")]
        [ProducesResponseType(typeof(OrderState), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult ValidateFill([FromBody] ValidateFillRequest request)
        {
            var result = orderManagementEngine.ValidateFill(request.Order, request.Execution);
            return Ok(result);
        }
 
        /// <summary>
        /// Cancels an existing order with a given reason.
        /// </summary>
        [HttpPost("cancel")]
        [ProducesResponseType(typeof(OrderState), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult Cancel([FromBody] CancelOrderRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Reason))
                return BadRequest("Cancellation reason cannot be empty.");
 
            var result = orderManagementEngine.Cancel(request.Order, request.Reason);
            return Ok(result);
        }
    }
 
    public record CreateOrderRequest(TradePlan Trade, PositionSizeResult Size, AccountContext Account);
    public record ValidateFillRequest(OrderRequest Order, ExecutionOrder Execution);
    public record CancelOrderRequest(OrderRequest Order, string Reason);
}