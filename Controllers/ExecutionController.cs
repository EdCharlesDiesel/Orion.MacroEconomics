using Microsoft.AspNetCore.Mvc;
using Orion.MacroEconomics.Engine.Interfaces;
using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Controllers;

[ApiController]
[Route("api/execution")]
[Produces("application/json")]
public sealed class ExecutionController(
    IExecutionEngine executionEngine,
    IAdvancedExecutionEngine advancedExecutionEngine,
    ILogger<ExecutionController> logger) : ControllerBase
{
    /// <summary>
    /// Executes a market order asynchronously using live market data.
    /// </summary>
    [HttpPost("execute")]
    [ProducesResponseType(typeof(ExecutionOrder), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ExecuteAsync(
        [FromBody] ExecuteOrderRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Pair))
            return BadRequest("Pair is required.");

        if (string.IsNullOrWhiteSpace(request.Direction))
            return BadRequest("Direction is required.");

        if (request.Size <= 0)
            return BadRequest("Size must be greater than zero.");

        logger.LogInformation("Executing order: {Pair} {Direction} {Size}", request.Pair, request.Direction, request.Size);

        var result = await executionEngine.ExecuteAsync(request.Pair, request.Direction, request.Size, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Executes an order against a provided order book snapshot.
    /// </summary>
    [HttpPost("execute/orderbook")]
    [ProducesResponseType(typeof(ExecutionOrder), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult ExecuteFromOrderBook([FromBody] ExecuteFromOrderBookRequest request)
    {
        if (request.OrderBook is null)
            return BadRequest("OrderBook is required.");

        if (string.IsNullOrWhiteSpace(request.Direction))
            return BadRequest("Direction is required.");

        if (request.Size <= 0)
            return BadRequest("Size must be greater than zero.");

        var result = executionEngine.Execute(request.OrderBook, request.Direction, request.Size);
        return Ok(result);
    }

    /// <summary>
    /// Executes a trade using the advanced execution engine with smart routing.
    /// </summary>
    [HttpPost("advanced/execute")]
    [ProducesResponseType(typeof(ExecutionResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AdvancedExecuteAsync(
        [FromBody] ExecuteOrderRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Pair))
            return BadRequest("Pair is required.");

        if (string.IsNullOrWhiteSpace(request.Direction))
            return BadRequest("Direction is required.");

        if (request.Size <= 0)
            return BadRequest("Size must be greater than zero.");

        logger.LogInformation("Advanced execution: {Pair} {Direction} {Size}", request.Pair, request.Direction, request.Size);

        var result = await advancedExecutionEngine.ExecuteAsync(request.Pair, request.Direction, request.Size, cancellationToken);
        return Ok(result);
    }
}

public sealed record ExecuteOrderRequest(string Pair, string Direction, decimal Size);
public sealed record ExecuteFromOrderBookRequest(OrderBook OrderBook, string Direction, decimal Size);