using Microsoft.AspNetCore.Mvc;
using Orion.MacroEconomics.DTO;
using Orion.MacroEconomics.Entities;
using Orion.MacroEconomics.Providers.Interfaces;

namespace Orion.MacroEconomics.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MarketDataController : ControllerBase
    {
        private readonly IEnumerable<IMarketDataFeedProvider> _providers;
        private readonly ILogger<MarketDataController> _logger;

        public MarketDataController(
            IEnumerable<IMarketDataFeedProvider> providers,
            ILogger<MarketDataController> logger)
        {
            _providers = providers;
            _logger = logger;
        }

        private IMarketDataFeedProvider GetProvider(string providerName)
        {
            var provider = _providers.FirstOrDefault(p =>
                p.Name.Equals(providerName, StringComparison.OrdinalIgnoreCase));
            
            if (provider == null)
                throw new ArgumentException($"Provider '{providerName}' not found. Available: {string.Join(", ", _providers.Select(p => p.Name))}");
            
            return provider;
        }

        /// <summary>
        /// List available data feed providers
        /// </summary>
        [HttpGet("providers")]
        public ActionResult<IEnumerable<string>> GetProviders()
        {
            return Ok(_providers.Select(p => p.Name));
        }

        /// <summary>
        /// Get raw data for a symbol from a specific provider
        /// </summary>
        [HttpGet("{providerName}/data")]
        public async Task<ActionResult<object>> GetAsync(
            string providerName,
            [FromQuery] string symbol,
            [FromQuery] DateTime fromUtc,
            [FromQuery] DateTime toUtc,
            CancellationToken cancellationToken)
        {
            try
            {
                var provider = GetProvider(providerName);
                var result = await provider.GetAsync(symbol, fromUtc, toUtc, cancellationToken);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting data from {Provider} for {Symbol}", providerName, symbol);
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Get macroeconomic data from a provider
        /// </summary>
        [HttpGet("{providerName}/macro")]
        public async Task<ActionResult<MacroData>> GetMacroDataAsync(
            string providerName,
            CancellationToken cancellationToken)
        {
            try
            {
                var provider = GetProvider(providerName);
                var result = await provider.GetMacroDataAsync(cancellationToken);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting macro data from {Provider}", providerName);
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Get FRED series mappings (if applicable)
        /// </summary>
        [HttpGet("{providerName}/seriesmappings")]
        public ActionResult<Dictionary<string, Dictionary<string, string>>> GetFredSeriesMappings(string providerName)
        {
            try
            {
                var provider = GetProvider(providerName);
                return Ok(provider.GetFredSeriesMappings());
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        /// <summary>
        /// Check provider status
        /// </summary>
        [HttpGet("{providerName}/status")]
        public async Task<ActionResult<FredStatusResponse>> CheckStatusAsync(
            string providerName,
            CancellationToken cancellationToken)
        {
            try
            {
                var provider = GetProvider(providerName);
                var status = await provider.CheckStatusAsync(cancellationToken);
                return Ok(status);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking status for {Provider}", providerName);
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Get historical OHLCV candles
        /// </summary>
        [HttpGet("{providerName}/candles")]
        public async Task<ActionResult<IReadOnlyList<OhlcvBar>>> GetHistoricalCandlesAsync(
            string providerName,
            [FromQuery] MarketDataRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                var provider = GetProvider(providerName);
                var candles = await provider.GetHistoricalCandlesAsync(request, cancellationToken);
                return Ok(candles);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting candles from {Provider} for {Pair}", providerName, request?.Pair);
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Get latest quote for a pair
        /// </summary>
        [HttpGet("{providerName}/quote/latest")]
        public async Task<ActionResult<MarketQuote>> GetLatestQuoteAsync(
            string providerName,
            [FromQuery] string pair,
            CancellationToken cancellationToken)
        {
            try
            {
                var provider = GetProvider(providerName);
                var quote = await provider.GetLatestQuoteAsync(pair, cancellationToken);
                if (quote == null) return NotFound($"No quote available for {pair}");
                return Ok(quote);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting latest quote from {Provider} for {Pair}", providerName, pair);
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Get latest tick for a pair
        /// </summary>
        [HttpGet("{providerName}/tick/latest")]
        public async Task<ActionResult<MarketTick>> GetLatestTickAsync(
            string providerName,
            [FromQuery] string pair,
            CancellationToken cancellationToken)
        {
            try
            {
                var provider = GetProvider(providerName);
                var tick = await provider.GetLatestTickAsync(pair, cancellationToken);
                if (tick == null) return NotFound($"No tick available for {pair}");
                return Ok(tick);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting latest tick from {Provider} for {Pair}", providerName, pair);
                return StatusCode(500, ex.Message);
            }
        }

        /// <summary>
        /// Check data health for a pair
        /// </summary>
        [HttpGet("{providerName}/health")]
        public async Task<ActionResult<MarketDataHealth>> CheckHealthAsync(
            string providerName,
            [FromQuery] string pair,
            CancellationToken cancellationToken)
        {
            try
            {
                var provider = GetProvider(providerName);
                var health = await provider.CheckHealthAsync(pair, cancellationToken);
                return Ok(health);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking health for {Provider} / {Pair}", providerName, pair);
                return StatusCode(500, ex.Message);
            }
        }
    }
}