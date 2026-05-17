using Microsoft.AspNetCore.Mvc;
using Orion.MacroEconomics.Engine.Interfaces;
using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Controllers
{
    [ApiController]
    [Route("api/market-replay")]
    [Produces("application/json")]
    public class MarketReplayController(IMarketReplayEngine marketReplayEngine) : ControllerBase
    {
        /// <summary>
        /// Replays candles sequentially in timestamp order.
        /// Note: IAsyncEnumerable is materialized into a list for REST compatibility.
        /// For real-time streaming, consider using SignalR or SSE instead.
        /// </summary>
        [HttpPost("replay")]
        [ProducesResponseType(typeof(List<Candle>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Replay(
            [FromBody] ReplayRequest request,
            CancellationToken cancellationToken)
        {
            if (!request.Candles.Any())
                return BadRequest("Candles list cannot be empty.");

            var result = new List<Candle>();

            await foreach (var candle in marketReplayEngine.ReplayAsync(
                request.Candles,
                request.DelayBetweenCandlesMs,
                cancellationToken))
            {
                result.Add(candle);
            }

            return Ok(result);
        }

        /// <summary>
        /// Replays candles in timestamp-ordered batches.
        /// Note: IAsyncEnumerable is materialized into a list for REST compatibility.
        /// For real-time streaming, consider using SignalR or SSE instead.
        /// </summary>
        [HttpPost("replay-batches")]
        [ProducesResponseType(typeof(List<List<Candle>>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ReplayBatches(
            [FromBody] ReplayBatchRequest request,
            CancellationToken cancellationToken)
        {
            if (!request.Candles.Any())
                return BadRequest("Candles list cannot be empty.");

            if (request.BatchSize <= 0)
                return BadRequest("BatchSize must be greater than zero.");

            var result = new List<IReadOnlyList<Candle>>();

            await foreach (var batch in marketReplayEngine.ReplayBatchesAsync(
                request.Candles,
                request.BatchSize,
                request.DelayBetweenBatchesMs,
                cancellationToken))
            {
                result.Add(batch);
            }

            return Ok(result);
        }
    }

    public record ReplayRequest(List<Candle> Candles, int DelayBetweenCandlesMs = 0);
    public record ReplayBatchRequest(List<Candle> Candles, int BatchSize = 100, int DelayBetweenBatchesMs = 0);
}