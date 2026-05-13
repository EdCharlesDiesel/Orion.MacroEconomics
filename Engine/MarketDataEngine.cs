using Orion.MacroEconomics.DTO;
using Orion.MacroEconomics.Engine.Interfaces;
using Orion.MacroEconomics.Entities;
using Orion.MacroEconomics.Interfaces;
using Orion.MacroEconomics.Providers.Interfaces;

namespace Orion.MacroEconomics.Engine
{
    public sealed class MarketDataEngine(IEnumerable<IMarketDataFeedProvider> providers, IMarketDataStore store, ILogger<MarketDataEngine> logger) : IMarketDataEngine
    {
        private IMarketDataFeedProvider GetProvider(string providerName)
        {
            var provider = providers.FirstOrDefault(x =>
                string.Equals(x.Name, providerName, StringComparison.OrdinalIgnoreCase));

            if (provider == null)
                throw new InvalidOperationException($"Provider '{providerName}' is not registered.");

            return provider;
        }

        public async Task<MacroData> GetMacroDataAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                logger.LogInformation("Getting macro data from Trading Economics");

                var tradingEconomics = GetProvider("TradingEconomics");

                return await tradingEconomics.GetMacroDataAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Trading Economics failed. Falling back to FRED.");

                var fred = GetProvider("FRED");

                var fallback = await fred.GetMacroDataAsync(cancellationToken);

                fallback.Warning = $"Trading Economics failed. Fallback used. {ex.Message}";
                fallback.DataSource = "FRED API fallback";

                return fallback;
            }
        }

        public async Task<MacroData> RefreshMacroDataAsync(CancellationToken cancellationToken = default)
        {
            return await GetMacroDataAsync(cancellationToken);
        }

        public Dictionary<string, Dictionary<string, string>> GetFredSeriesMappings()
        {
            var fred = GetProvider("FRED");

            return fred.GetFredSeriesMappings();
        }

        public async Task<FredStatusResponse> CheckStatusAsync(CancellationToken cancellationToken = default)
        {
            var fred = GetProvider("FRED");

            return await fred.CheckStatusAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<OhlcvBar>> GetHistoricalCandlesAsync(MarketDataRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            var providerName = string.IsNullOrWhiteSpace(request.Provider)
                ? "Yahoo"
                : request.Provider;

            var provider = GetProvider(providerName);

            return await provider.GetHistoricalCandlesAsync(request, cancellationToken);
        }

        public async Task<MarketQuote?> GetLatestQuoteAsync(string pair, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(pair))
                throw new ArgumentException("Pair is required.", nameof(pair));

            var provider = GetProvider("Yahoo");

            return await provider.GetLatestQuoteAsync(pair, cancellationToken);
        }

        public async Task<MarketTick?> GetLatestTickAsync(string pair, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(pair))
                throw new ArgumentException("Pair is required.", nameof(pair));

            MarketTick? tick = null;

            try
            {
                var dukascopy = GetProvider("Dukascopy");
                tick = await dukascopy.GetLatestTickAsync(pair, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Dukascopy failed. Falling back to TrueFX.");
            }

            if (tick != null)
                return tick;

            var trueFx = GetProvider("TrueFX");

            return await trueFx.GetLatestTickAsync(pair, cancellationToken);
        }

        public async Task<MarketDataHealth> CheckHealthAsync(string pair, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(pair))
                throw new ArgumentException("Pair is required.", nameof(pair));

            var results = new List<MarketDataHealth>();

            foreach (var provider in providers)
            {
                try
                {
                    var health = await provider.CheckHealthAsync(pair, cancellationToken);
                    results.Add(health);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Health check failed for provider {Provider}", provider.Name);
                }
            }

            return new MarketDataHealth
            {
                Provider = "MarketDataEngine",
                Pair = pair.ToUpperInvariant(),
                IsHealthy = results.Any(x => x.IsHealthy),
                Message = results.Any(x => x.IsHealthy)
                    ? "At least one market data provider is healthy."
                    : "No market data providers are healthy.",
                CheckedAtUtc = DateTime.UtcNow
            };
        }

        public async Task<MarketDataSnapshot> FetchAndStoreAsync(string provider, string symbol, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(provider))
                throw new ArgumentException("Provider is required.", nameof(provider));

            if (string.IsNullOrWhiteSpace(symbol))
                throw new ArgumentException("Symbol is required.", nameof(symbol));

            if (fromUtc >= toUtc)
                throw new ArgumentException("fromUtc must be earlier than toUtc.");

            var selectedProvider = GetProvider(provider);

            logger.LogInformation(
                "Fetching market data from {Provider} for {Symbol}",
                selectedProvider.Name,
                symbol);

            var payload = await selectedProvider.GetAsync(
                symbol,
                fromUtc,
                toUtc,
                cancellationToken);

            var dataType = selectedProvider.Name switch
            {
                "FRED" => "Macro",
                "TradingEconomics" => "Event",
                "Dukascopy" => "Tick",
                "TrueFX" => "Tick",
                "Yahoo" => "Candle",
                _ => "Unknown"
            };

            return await store.SaveAsync(
                selectedProvider.Name,
                dataType,
                symbol,
                fromUtc,
                toUtc,
                payload,
                cancellationToken);
        }
    }
}