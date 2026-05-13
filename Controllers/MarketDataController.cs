using Microsoft.AspNetCore.Mvc;
using Orion.MacroEconomics.DTO;
using Orion.MacroEconomics.Engine.Interfaces;
using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Controllers
{
    [ApiController]
    [Route("api/market-data")]
    [Produces("application/json")]
    public class MarketDataController(IMarketDataEngine marketDataEngine) : ControllerBase
    {
        /// <summary>
        /// Fetches and stores market data for a given provider, symbol, and date range.
        /// </summary>
        [HttpPost("fetch")]
        [ProducesResponseType(typeof(MarketDataSnapshot), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> FetchAndStore(
            [FromBody] FetchMarketDataRequest request,
            CancellationToken cancellationToken)
        {
            var result = await marketDataEngine.FetchAndStoreAsync(
                request.Provider,
                request.Symbol,
                request.FromUtc,
                request.ToUtc,
                cancellationToken);

            return Ok(result);
        }

        /// <summary>
        /// Returns the current cached macro data.
        /// </summary>
        [HttpGet("macro")]
        [ProducesResponseType(typeof(MacroData), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMacroData(CancellationToken cancellationToken)
        {
            var result = await marketDataEngine.GetMacroDataAsync(cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Forces a refresh of macro data from the upstream source.
        /// </summary>
        [HttpPost("macro/refresh")]
        [ProducesResponseType(typeof(MacroData), StatusCodes.Status200OK)]
        public async Task<IActionResult> RefreshMacroData(CancellationToken cancellationToken)
        {
            var result = await marketDataEngine.RefreshMacroDataAsync(cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Returns FRED series mappings used for macro data retrieval.
        /// </summary>
        [HttpGet("fred-series")]
        [ProducesResponseType(typeof(Dictionary<string, Dictionary<string, string>>), StatusCodes.Status200OK)]
        public IActionResult GetFredSeriesMappings()
        {
            var result = marketDataEngine.GetFredSeriesMappings();
            return Ok(result);
        }

        /// <summary>
        /// Checks the status of the FRED data source.
        /// </summary>
        [HttpGet("fred-status")]
        [ProducesResponseType(typeof(FredStatusResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> CheckFredStatus(CancellationToken cancellationToken)
        {
            var result = await marketDataEngine.CheckStatusAsync(cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Returns historical OHLCV candles for the given market data request.
        /// </summary>
        [HttpPost("historical")]
        [ProducesResponseType(typeof(IReadOnlyList<OhlcvBar>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetHistoricalCandles(
            [FromBody] MarketDataRequest request,
            CancellationToken cancellationToken)
        {
            var result = await marketDataEngine.GetHistoricalCandlesAsync(request, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Returns the latest market quote for a currency pair.
        /// </summary>
        [HttpGet("quote/{pair}")]
        [ProducesResponseType(typeof(MarketQuote), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetLatestQuote(string pair, CancellationToken cancellationToken)
        {
            var result = await marketDataEngine.GetLatestQuoteAsync(pair, cancellationToken);
            return result is null ? NotFound($"No quote found for pair '{pair}'.") : Ok(result);
        }

        /// <summary>
        /// Checks market data health for a currency pair.
        /// </summary>
        [HttpGet("health/{pair}")]
        [ProducesResponseType(typeof(MarketDataHealth), StatusCodes.Status200OK)]
        public async Task<IActionResult> CheckHealth(string pair, CancellationToken cancellationToken)
        {
            var result = await marketDataEngine.CheckHealthAsync(pair, cancellationToken);
            return Ok(result);
        }

        /// <summary>
        /// Returns the latest market tick for a currency pair.
        /// </summary>
        [HttpGet("tick/{pair}")]
        [ProducesResponseType(typeof(MarketTick), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetLatestTick(string pair, CancellationToken cancellationToken)
        {
            var result = await marketDataEngine.GetLatestTickAsync(pair, cancellationToken);
            return Ok(result);
        }
    }

    public record FetchMarketDataRequest(string Provider, string Symbol, DateTime FromUtc, DateTime ToUtc);
}