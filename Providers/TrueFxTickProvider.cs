using System.Globalization;
using Orion.MacroEconomics.DTO;
using Orion.MacroEconomics.Entities;
using Orion.MacroEconomics.Providers.Interfaces;

namespace Orion.MacroEconomics.Providers;

public sealed class TrueFxTickProvider(IConfiguration configuration, ILogger<TrueFxTickProvider> logger) : ITrueFxTickProvider
{
    public string Name => "TrueFX";

    public bool CanHandle(MarketDataRequest? request)
    {
        return request != null &&
               string.Equals(request.Provider, Name, StringComparison.OrdinalIgnoreCase);
    }

    public async Task<ProviderDataResult> FetchAsync(MarketDataRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var ticks = await ReadTicksAsync(request.Pair, request.From, request.To, cancellationToken);

        return new ProviderDataResult
        {
            Provider = Name,
            Symbol = request.Pair.Trim().ToUpperInvariant(),
            Payload = [ticks],
            Success = true,
            Message = ticks.Count > 0
                ? "TrueFX tick data loaded successfully."
                : "No TrueFX tick data found."
        };
    }

    public Task<FredStatusResponse> CheckStatusAsync(CancellationToken cancellationToken = default)
    {
        var folder = configuration["TrueFx:DataFolder"];

        var isHealthy = !string.IsNullOrWhiteSpace(folder) &&
                        Directory.Exists(folder);

        return Task.FromResult(new FredStatusResponse
        {
            IsConnected = isHealthy,
            Source = Name,
            Message = isHealthy
                ? "TrueFX data folder is available."
                : "TrueFX data folder is missing or invalid.",
            CheckedAtUtc = DateTime.UtcNow
        });
    }

    public async Task<IReadOnlyList<OhlcvBar>> GetHistoricalCandlesAsync(MarketDataRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var ticks = await ReadTicksAsync(
            request.Pair,
            request.From,
            request.To,
            cancellationToken);

        return ticks
            .GroupBy(x => new DateTime(
                x.Time.Year,
                x.Time.Month,
                x.Time.Day,
                x.Time.Hour,
                0,
                0,
                DateTimeKind.Utc))
            .OrderBy(x => x.Key)
            .Select(group =>
            {
                var ordered = group.OrderBy(x => x.Time).ToList();

                return new OhlcvBar
                {
                    Pair = request.Pair.Trim().ToUpperInvariant(),
                    TimestampUtc = group.Key,
                    Open = (ordered.First().Bid + ordered.First().Ask) / 2m,    // Calculate mid from Bid/Ask
                    High = ordered.Max(x => (x.Bid + x.Ask) / 2m),             // Max of mid prices
                    Low = ordered.Min(x => (x.Bid + x.Ask) / 2m),              // Min of mid prices
                    Close = (ordered.Last().Bid + ordered.Last().Ask) / 2m,    // Calculate mid from Bid/Ask
                    Volume = ordered.Count
                };
            })
            .ToList();
    }
    public async Task<MarketQuote?> GetLatestQuoteAsync(string pair, CancellationToken cancellationToken = default)
    {
        var tick = await GetLatestTickAsync(pair, cancellationToken);

        if (tick == null)
            return null;

        return new MarketQuote
        {
            Pair = tick.Pair,
            Bid = tick.Bid,
            Ask = tick.Ask,
            Last = (tick.Bid + tick.Ask) / 2m,
            TimestampUtc = tick.Time,
            Source = Name
        };
    }

    public async Task<MarketTick?> GetLatestTickAsync(string pair, CancellationToken cancellationToken = default)
    {
        var ticks = await ReadTicksAsync(
            pair,
            DateTime.MinValue,
            DateTime.UtcNow,
            cancellationToken);

        return ticks
            .OrderByDescending(x => x.Time)
            .FirstOrDefault();
    }

    public async Task<MarketDataHealth> CheckHealthAsync(string pair, CancellationToken cancellationToken = default)
    {
        try
        {
            var tick = await GetLatestTickAsync(pair, cancellationToken);

            return new MarketDataHealth
            {
                Provider = Name,
                Pair = pair.Trim().ToUpperInvariant(),
                IsHealthy = tick != null,
                Message = tick != null
                    ? "TrueFX provider is healthy."
                    : "TrueFX provider returned no tick data.",
                CheckedAtUtc = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "TrueFX health check failed for {Pair}", pair);

            return new MarketDataHealth
            {
                Provider = Name,
                Pair = pair.Trim().ToUpperInvariant(),
                IsHealthy = false,
                Message = ex.Message,
                CheckedAtUtc = DateTime.UtcNow
            };
        }
    }

    public async Task<object> GetAsync(
        string symbol,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default)
    {
        return await ReadTicksAsync(
            symbol,
            fromUtc,
            toUtc,
            cancellationToken);
    }

    public Task<MacroData> GetMacroDataAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new MacroData
        {
            DataSource = Name,
            Warning = "TrueFX is a tick data provider, not a macro data provider."
        });
    }

    public Dictionary<string, Dictionary<string, string>> GetFredSeriesMappings()
    {
        return new Dictionary<string, Dictionary<string, string>>();
    }

    private async Task<List<MarketTick>> ReadTicksAsync(string pair, DateTime? fromUtc, DateTime? toUtc, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(pair))
            throw new ArgumentException("Pair is required.", nameof(pair));

        var folder = configuration["TrueFx:DataFolder"];

        if (string.IsNullOrWhiteSpace(folder))
            throw new InvalidOperationException("TrueFX data folder is missing.");

        if (!Directory.Exists(folder))
            throw new DirectoryNotFoundException($"TrueFX data folder does not exist: {folder}");

        var symbol = NormalizePair(pair);
        var file = Path.Combine(folder, $"{symbol}.csv");

        if (!File.Exists(file))
            return [];

        var lines = await File.ReadAllLinesAsync(file, cancellationToken);

        return lines
            .Skip(1)
            .Select(line => line.Split(','))
            .Where(x => x.Length >= 3)
            .Select(x => TryParseTick(symbol, x))
            .Where(x => x != null)
            .Select(x => x!)
            .Where(x =>
                (fromUtc == default || x.Time >= fromUtc) &&
                (toUtc == default || x.Time <= toUtc))
            .OrderBy(x => x.Time)
            .ToList();
    }

    private static MarketTick? TryParseTick(string symbol, string[] columns)
    {
        if (!DateTime.TryParse(
                columns[0],
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out var timestampUtc))
            return null;

        if (!decimal.TryParse(
                columns[1],
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out var bid))
            return null;

        if (!decimal.TryParse(
                columns[2],
                NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out var ask))
            return null;

        return new MarketTick
        {
            Pair = symbol,
            Bid = bid,
            Ask = ask,
            Time = timestampUtc,
            Source = "TrueFX"
        };
    }

    private static string NormalizePair(string pair)
    {
        var clean = pair
            .Trim()
            .ToUpperInvariant()
            .Replace("/", "")
            .Replace("-", "")
            .Replace("=X", "");

        if (clean.Length != 6)
            throw new ArgumentException(
                $"Invalid FX pair '{pair}'. Expected format like EUR/USD or EURUSD.");

        return clean;
    }
}