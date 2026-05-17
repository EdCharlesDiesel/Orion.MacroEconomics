using Orion.MacroEconomics.DTO;
using Orion.MacroEconomics.Entities;
using Orion.MacroEconomics.Interfaces;
using Orion.MacroEconomics.Providers.Interfaces;

namespace Orion.MacroEconomics.Providers;

public sealed class FredMacroProvider(IFredService fredService, ILogger<FredMacroProvider> logger) : IFredMacroProvider
{
    public string Name => "FRED";

    public bool CanHandle(MarketDataRequest request)
    {
        return request != null &&
               string.Equals(request.Provider, Name, StringComparison.OrdinalIgnoreCase);
    }

    public async Task<ProviderDataResult> FetchAsync(
        MarketDataRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var data = await fredService.GetMacroDataAsync(cancellationToken);

        return new ProviderDataResult
        {
            Provider = Name,
            Symbol = string.IsNullOrWhiteSpace(request.Pair)
                ? "MACRO"
                : request.Pair.Trim().ToUpperInvariant(),
            Payload = new List<object>() { data },
            Success = true,
            Message = "FRED macro data loaded successfully."
        };
    }

    public async Task<object> GetAsync(
        string symbol,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            return await fredService.GetMacroDataAsync(cancellationToken);

        var clean = symbol.Trim().ToUpperInvariant();

        if (clean.Length == 3)
            return await fredService.GetCurrencyMacroDataAsync(clean, cancellationToken);

        return await fredService.GetMacroDataAsync(cancellationToken);
    }

    public Task<MacroData> GetMacroDataAsync(
        CancellationToken cancellationToken = default)
    {
        return fredService.GetMacroDataAsync(cancellationToken);
    }

    public Dictionary<string, Dictionary<string, string>> GetFredSeriesMappings()
    {
        return fredService.GetFredSeriesMappings();
    }

    public Task<FredStatusResponse> CheckStatusAsync(
        CancellationToken cancellationToken = default)
    {
        return fredService.CheckStatusAsync(cancellationToken);
    }

    public Task<IReadOnlyList<OhlcvBar>> GetHistoricalCandlesAsync(
        MarketDataRequest request,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("FRED does not provide OHLCV candle data.");

        return Task.FromResult<IReadOnlyList<OhlcvBar>>([]);
    }

    public Task<MarketQuote?> GetLatestQuoteAsync(
        string pair,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("FRED does not provide latest market quotes.");

        return Task.FromResult<MarketQuote?>(null);
    }

    public Task<MarketTick?> GetLatestTickAsync(
        string pair,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("FRED does not provide tick data.");

        return Task.FromResult<MarketTick?>(null);
    }

    public async Task<MarketDataHealth> CheckHealthAsync(
        string pair,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var status = await fredService.CheckStatusAsync(cancellationToken);

            return new MarketDataHealth
            {
                Provider = Name,
                Pair = string.IsNullOrWhiteSpace(pair)
                    ? "MACRO"
                    : pair.Trim().ToUpperInvariant(),
                IsHealthy = status.IsConnected,
                Message = status.Message,
                CheckedAtUtc = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "FRED health check failed.");

            return new MarketDataHealth
            {
                Provider = Name,
                Pair = string.IsNullOrWhiteSpace(pair)
                    ? "MACRO"
                    : pair.Trim().ToUpperInvariant(),
                IsHealthy = false,
                Message = ex.Message,
                CheckedAtUtc = DateTime.UtcNow
            };
        }
    }
}