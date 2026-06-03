using System.Text.Json;
using Orion.MacroEconomics.DTO;
using Orion.MacroEconomics.Engine.Interfaces;
using Orion.MacroEconomics.Entities;
using Orion.MacroEconomics.Providers.Interfaces;
using Orion.MacroEconomics.Repository.Interfaces;

namespace Orion.MacroEconomics.Engine;

public sealed class MarketDataEngine(
    IEnumerable<IMarketDataFeedProvider> providers,
    IMarketDataRepository store,
    ILogger<MarketDataEngine> logger) : IMarketDataEngine
{
    private IMarketDataFeedProvider GetProvider(string providerName) =>
        providers.FirstOrDefault(x =>
            string.Equals(x.Name, providerName, StringComparison.OrdinalIgnoreCase))
        ?? throw new InvalidOperationException($"Provider '{providerName}' is not registered.");

    public async Task<MacroData?> GetMacroDataAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Getting macro data from Trading Economics");
            return await GetProvider("TradingEconomics").GetMacroDataAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Trading Economics failed. Falling back to FRED.");
            var fallback = await GetProvider("FRED").GetMacroDataAsync(cancellationToken);
            fallback.Warning    = $"Trading Economics failed. Fallback used. {ex.Message}";
            fallback.DataSource = "FRED API fallback";
            return fallback;
        }
    }

    public Task<MacroData?> RefreshMacroDataAsync(CancellationToken cancellationToken = default)
        => GetMacroDataAsync(cancellationToken);

    public Dictionary<string, Dictionary<string, string>> GetFredSeriesMappings()
        => GetProvider("FRED").GetFredSeriesMappings();

    public Task<FredStatusResponse> CheckStatusAsync(CancellationToken cancellationToken = default)
        => GetProvider("FRED").CheckStatusAsync(cancellationToken);

    public async Task<IReadOnlyList<OhlcvBar>> GetHistoricalCandlesAsync(
        MarketDataRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var providerName = string.IsNullOrWhiteSpace(request.Provider) ? "Yahoo" : request.Provider;
        return await GetProvider(providerName).GetHistoricalCandlesAsync(request, cancellationToken);
    }

    public async Task<MarketQuote?> GetLatestQuoteAsync(
        string pair, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pair);
        return await GetProvider("Yahoo").GetLatestQuoteAsync(pair, cancellationToken);
    }

    public async Task<MarketTick?> GetLatestTickAsync(
        string pair, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pair);
        try
        {
            var tick = await GetProvider("Dukascopy").GetLatestTickAsync(pair, cancellationToken);
            if (tick != null) return tick;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Dukascopy failed. Falling back to TrueFX.");
        }
        return await GetProvider("TrueFX").GetLatestTickAsync(pair, cancellationToken);
    }

    public async Task<MarketDataSnapshot> FetchAndStoreAsync(
        string provider, string symbol,
        DateTime fromUtc, DateTime toUtc,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(provider);
        ArgumentException.ThrowIfNullOrWhiteSpace(symbol);
        if (fromUtc >= toUtc)
            throw new ArgumentException("fromUtc must be earlier than toUtc.");

        var selectedProvider = GetProvider(provider);
        logger.LogInformation("Fetching from {Provider} for {Symbol}", selectedProvider.Name, symbol);

        object payload  = await selectedProvider.GetAsync(symbol, fromUtc, toUtc, cancellationToken);
        var dataType = selectedProvider.Name switch
        {
            "FRED"             => "Macro",
            "TradingEconomics" => "Event",
            "Dukascopy"        => "Tick",
            "TrueFX"           => "Tick",
            "Yahoo"            => "Candle",
            _                  => "Unknown"
        };

        return await store.SaveAsync(
            selectedProvider.Name, dataType, symbol,
            fromUtc, toUtc, payload, cancellationToken);
    }

    public async Task<MarketDataHealth?> CheckHealthAsync(
        string pair, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pair);
        var results = new List<MarketDataHealth>();
        foreach (var provider in providers)
        {
            try { results.Add(await provider.CheckHealthAsync(pair, cancellationToken)); }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Health check failed for {Provider}", provider.Name);
            }
        }
        var anyHealthy = results.Any(x => x.IsHealthy);
        return new MarketDataHealth
        {
            Provider     = "MarketDataEngine",
            Pair         = pair.ToUpperInvariant(),
            IsHealthy    = anyHealthy,
            Message      = anyHealthy
                ? "At least one provider is healthy."
                : "No providers are healthy.",
            CheckedAtUtc = DateTime.UtcNow
        };
    }

    // ── Delegated directly to repository ──────────────────────────────────────
    public Task<MarketDataSnapshot> SaveAsync(
        string providerName, string dataType, string symbol,
        DateTime fromUtc, DateTime toUtc,
        object payload, CancellationToken cancellationToken)
        => store.SaveAsync(providerName, dataType, symbol, fromUtc, toUtc, payload, cancellationToken);

    public Task<MarketDataSnapshot?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => store.GetByIdAsync(id, cancellationToken);

    public Task<IReadOnlyList<MarketDataSnapshot>> GetBySymbolAsync(
        string symbol, DateTime? fromUtc = null, DateTime? toUtc = null,
        CancellationToken cancellationToken = default)
        => store.GetBySymbolAsync(symbol, fromUtc, toUtc, cancellationToken);

    public Task<IReadOnlyList<MarketDataSnapshot>> GetByProviderAsync(
        string providerName, string? dataType = null,
        CancellationToken cancellationToken = default)
        => store.GetByProviderAsync(providerName, dataType, cancellationToken);

    public Task<bool> ExistsAsync(
        string symbol, DateTime fromUtc, DateTime toUtc,
        CancellationToken cancellationToken = default)
        => store.ExistsAsync(symbol, fromUtc, toUtc, cancellationToken);

    public Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => store.DeleteAsync(id, cancellationToken);
}