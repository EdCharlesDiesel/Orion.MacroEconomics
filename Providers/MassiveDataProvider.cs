using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Orion.MacroEconomics.Configurations;
using Orion.MacroEconomics.Entities;
using Orion.MacroEconomics.Models;
using Orion.MacroEconomics.Providers.Interfaces;

namespace Orion.MacroEconomics.Providers;

public sealed class MassiveDataProvider : IMassiveDataProvider
{
    private readonly HttpClient                   _http;
    private readonly ILogger<MassiveDataProvider> _log;

    // Max 3 concurrent requests to Massive at any time
    private static readonly SemaphoreSlim _throttle = new(3, 3);

    private static readonly JsonSerializerOptions _json =
        new() { PropertyNameCaseInsensitive = true };

    public MassiveDataProvider(
        IHttpClientFactory           httpClientFactory,
        IConfiguration               config,
        ILogger<MassiveDataProvider> log)
    {
        _log  = log;
        _http = httpClientFactory.CreateClient("Massive");

        var apiKey = config["Massive:ApiKey"]
            ?? throw new InvalidOperationException("Missing Massive:ApiKey in configuration.");

        _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
    }

    // ── Candles ────────────────────────────────────────────────────────────────
    public async Task<List<Candle>> GetCandlesAsync(
        string pair, string timeframe, CancellationToken ct = default)
    {
        if (!AppConfig.Assets.TryGetValue(pair, out var ticker))
        {
            _log.LogWarning("No Massive ticker for pair '{Pair}'", pair);
            return [];
        }

        if (!AppConfig.Timeframes.TryGetValue(timeframe, out var tf))
        {
            _log.LogWarning("Unknown timeframe '{Tf}'", timeframe);
            return [];
        }

        var (mult, span, days) = tf;
        var to   = DateOnly.FromDateTime(DateTime.UtcNow);
        var from = to.AddDays(-days);
        var url  = $"/v2/aggs/ticker/{ticker}/range/{mult}/{span}/{from:yyyy-MM-dd}/{to:yyyy-MM-dd}"
                 + "?adjusted=true&sort=asc&limit=50000";

        try
        {
            var resp = await SendWithRetryAsync(url, ct);
            if (resp is null) return [];

            var body = await resp.Content.ReadAsStringAsync(ct);
            var agg  = JsonSerializer.Deserialize<MassiveAggResponse>(body, _json);

            if (agg?.Results is null || agg.Results.Count == 0)
            {
                _log.LogWarning("No bars returned for {Pair}/{Tf}", pair, timeframe);
                return [];
            }

            return agg.Results.Select(r => new Candle
            {
                Time   = DateTimeOffset.FromUnixTimeMilliseconds(r.T).UtcDateTime,
                Open   = r.O,
                High   = r.H,
                Low    = r.L,
                Close  = r.C,
                Volume = r.V
            }).ToList();
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Massive fetch failed for {Pair}/{Tf}", pair, timeframe);
            return [];
        }
    }

    public async Task<(decimal Price, decimal ChangePercent)> GetSnapshotAsync(
        string pair, CancellationToken ct = default)
    {
        var candles = await GetCandlesAsync(pair, "Daily", ct);
        if (candles.Count < 2) return (0, 0);
        var price  = (decimal)candles[^1].Close;
        var prev   = (decimal)candles[^2].Close;
        var change = prev > 0 ? (price - prev) / prev * 100 : 0;
        return (price, change);
    }

    public async Task<List<OhlcvBar>> FetchDataAsync(
        string pair, string timeframe, string resolution,
        CancellationToken ct)
    {
        var candles = await GetCandlesAsync(pair, timeframe, ct);
        return candles.Select(c => new OhlcvBar
        {
            TimestampUtc = c.Time,
            Open         = c.Open,
            High         = c.High,
            Low          = c.Low,
            Close        = c.Close,
            Volume       = c.Volume
        }).ToList();
    }

    // ── Macro ──────────────────────────────────────────────────────────────────
    public static readonly Dictionary<string, MacroSnapshot> Fallbacks = new()
    {
        { "USD", new() { Currency = "USD", GDP = 2.5m,  Inflation = 3.2m, Rates = 5.50m,  Unemployment = 3.8m  } },
        { "EUR", new() { Currency = "EUR", GDP = 0.8m,  Inflation = 2.9m, Rates = 4.50m,  Unemployment = 6.5m  } },
        { "GBP", new() { Currency = "GBP", GDP = 0.6m,  Inflation = 3.4m, Rates = 5.25m,  Unemployment = 4.2m  } },
        { "JPY", new() { Currency = "JPY", GDP = 1.1m,  Inflation = 2.8m, Rates = -0.10m, Unemployment = 2.6m  } },
        { "ZAR", new() { Currency = "ZAR", GDP = 1.2m,  Inflation = 5.0m, Rates = 8.25m,  Unemployment = 32.1m } },
        { "AUD", new() { Currency = "AUD", GDP = 2.0m,  Inflation = 4.1m, Rates = 4.35m,  Unemployment = 3.9m  } },
        { "NZD", new() { Currency = "NZD", GDP = 2.2m,  Inflation = 3.8m, Rates = 5.50m,  Unemployment = 3.9m  } },
        { "CAD", new() { Currency = "CAD", GDP = 1.5m,  Inflation = 3.4m, Rates = 5.00m,  Unemployment = 5.1m  } },
        { "CHF", new() { Currency = "CHF", GDP = 0.9m,  Inflation = 2.1m, Rates = 1.75m,  Unemployment = 2.0m  } },
    };

    public async Task<(List<MacroSnapshot> Data, bool IsLive)> GetAllMacroAsync(
        CancellationToken ct = default)
    {
        var result = Fallbacks.Values.Select(Clone).ToList();
        var isLive = false;

        try
        {
            var gdp  = await GetLatestIndicatorAsync("gdp",                "quarterly", ct);
            var cpi  = await GetLatestIndicatorAsync("cpi",                "monthly",   ct);
            var rate = await GetLatestIndicatorAsync("federal_funds_rate", "monthly",   ct);
            var unem = await GetLatestIndicatorAsync("unemployment",       "monthly",   ct);

            if (gdp.HasValue || cpi.HasValue || rate.HasValue || unem.HasValue)
            {
                var usd = result.First(r => r.Currency == "USD");
                if (gdp.HasValue)  usd.GDP          = (decimal)Math.Round(gdp.Value,  2);
                if (cpi.HasValue)  usd.Inflation    = (decimal)Math.Round(cpi.Value,  2);
                if (rate.HasValue) usd.Rates        = (decimal)Math.Round(rate.Value, 2);
                if (unem.HasValue) usd.Unemployment = (decimal)Math.Round(unem.Value, 2);
                isLive = true;
            }
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Live macro fetch failed — using fallback values");
        }

        return (result, isLive);
    }

    // ── Series ─────────────────────────────────────────────────────────────────
    public async Task<List<EconomyDataPoint>> GetSeriesAsync(
        string seriesId, int limit = 36, CancellationToken ct = default)
    {
        try
        {
            return seriesId switch
            {
                "FEDFUNDS"        => await GetIndicatorSeriesAsync("federal_funds_rate", "monthly",   limit, ct),
                "CPIAUCSL"        => await GetIndicatorSeriesAsync("cpi",                "monthly",   limit, ct),
                "UNRATE"          => await GetIndicatorSeriesAsync("unemployment",       "monthly",   limit, ct),
                "A191RL1Q225SBEA" => await GetIndicatorSeriesAsync("gdp",                "quarterly", limit, ct),
                "DGS10"           => await GetIndicatorSeriesAsync("treasury_yield",     "daily",     limit, ct, "10Y"),
                "DGS2"            => await GetIndicatorSeriesAsync("treasury_yield",     "daily",     limit, ct, "2Y"),
                "DTWEXBGS"        => await GetIndexSeriesAsync("I:DXY", limit, ct),
                "VIXCLS"          => await GetIndexSeriesAsync("I:VIX", limit, ct),
                _                 => []
            };
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Economy series '{Id}' failed", seriesId);
            return [];
        }
    }

    // ── Core retry + throttle logic ────────────────────────────────────────────

    /// <summary>
    /// Sends a GET request with:
    /// - Semaphore throttling (max 3 concurrent)
    /// - 200ms gap between requests
    /// - Exponential backoff on 429 (up to 4 retries)
    /// - Respects Retry-After header if present
    /// Returns null if all retries are exhausted.
    /// </summary>
    private async Task<HttpResponseMessage?> SendWithRetryAsync(
        string url, CancellationToken ct, int maxRetries = 4)
    {
        await _throttle.WaitAsync(ct);
        try
        {
            for (var attempt = 1; attempt <= maxRetries; attempt++)
            {
                var resp = await _http.GetAsync(url, ct);

                if (resp.StatusCode != HttpStatusCode.TooManyRequests)
                {
                    resp.EnsureSuccessStatusCode();
                    await Task.Delay(200, ct); // 200ms gap between successful requests
                    return resp;
                }

                if (attempt == maxRetries)
                {
                    _log.LogError(
                        "Rate limit (429) not resolved after {Max} retries for {Url}",
                        maxRetries, url);
                    return null;
                }

                // Respect Retry-After if server sends it, else exponential backoff
                var retryAfter = resp.Headers.RetryAfter?.Delta
                              ?? TimeSpan.FromSeconds(Math.Pow(2, attempt)); // 2s, 4s, 8s, 16s

                _log.LogWarning(
                    "Rate limited (429). Waiting {Delay:F1}s before retry {Attempt}/{Max} for {Url}",
                    retryAfter.TotalSeconds, attempt, maxRetries, url);

                await Task.Delay(retryAfter, ct);
            }

            return null;
        }
        finally
        {
            _throttle.Release();
        }
    }

    // ── Private helpers ────────────────────────────────────────────────────────
    private async Task<double?> GetLatestIndicatorAsync(
        string slug, string timespan, CancellationToken ct)
    {
        var resp = await SendWithRetryAsync(
            $"/v1/indicatordata/{slug}?timespan={timespan}&limit=1", ct);
        if (resp is null) return null;

        var body   = await resp.Content.ReadAsStringAsync(ct);
        var parsed = JsonSerializer.Deserialize<IndicatorResponse>(body, _json);
        var data   = parsed?.Results?.Data;
        return data is { Count: > 0 } ? data[^1].Value : null;
    }

    private async Task<List<EconomyDataPoint>> GetIndicatorSeriesAsync(
        string slug, string timespan, int limit,
        CancellationToken ct, string? yieldCurve = null)
    {
        var query = $"?timespan={timespan}&limit={limit}";
        if (yieldCurve is not null) query += $"&yield_curve={yieldCurve}";

        var resp = await SendWithRetryAsync($"/v1/indicatordata/{slug}{query}", ct);
        if (resp is null) return [];

        var body   = await resp.Content.ReadAsStringAsync(ct);
        var parsed = JsonSerializer.Deserialize<IndicatorResponse>(body, _json);

        return parsed?.Results?.Data?
            .Where(d => d.Value.HasValue)
            .Select(d => new EconomyDataPoint
            {
                Date  = d.Date,
                Value = (decimal)d.Value!.Value
            })
            .OrderBy(d => d.Date)
            .ToList() ?? [];
    }

    private async Task<List<EconomyDataPoint>> GetIndexSeriesAsync(
        string ticker, int limit, CancellationToken ct)
    {
        var to   = DateOnly.FromDateTime(DateTime.UtcNow);
        var from = to.AddDays(-limit * 2);
        var url  = $"/v2/aggs/ticker/{ticker}/range/1/day/{from:yyyy-MM-dd}/{to:yyyy-MM-dd}"
                 + $"?sort=asc&limit={limit}";

        var resp = await SendWithRetryAsync(url, ct);
        if (resp is null) return [];

        var body   = await resp.Content.ReadAsStringAsync(ct);
        var parsed = JsonSerializer.Deserialize<MassiveAggResponse>(body, _json);

        return parsed?.Results?
            .Select(r => new EconomyDataPoint
            {
                Date  = DateTimeOffset.FromUnixTimeMilliseconds(r.T).UtcDateTime.Date,
                Value = r.C
            })
            .ToList() ?? [];
    }

    private static MacroSnapshot Clone(MacroSnapshot s) => new()
    {
        Currency     = s.Currency,
        GDP          = s.GDP,
        Inflation    = s.Inflation,
        Rates        = s.Rates,
        Unemployment = s.Unemployment
    };

    // ── Response models ────────────────────────────────────────────────────────
    private sealed class IndicatorResponse
    {
        [JsonPropertyName("results")] public IndicatorResults? Results { get; set; }
    }

    private sealed class IndicatorResults
    {
        [JsonPropertyName("data")] public List<DataPoint>? Data { get; set; }
    }

    private sealed class DataPoint
    {
        [JsonPropertyName("date")]  public DateTime Date  { get; set; }
        [JsonPropertyName("value")] public double?  Value { get; set; }
    }

    private sealed class MassiveAggResponse
    {
        [JsonPropertyName("results")] public List<AggBar>? Results { get; set; }
    }

    private sealed class AggBar
    {
        [JsonPropertyName("o")] public decimal O { get; set; }
        [JsonPropertyName("h")] public decimal H { get; set; }
        [JsonPropertyName("l")] public decimal L { get; set; }
        [JsonPropertyName("c")] public decimal C { get; set; }
        [JsonPropertyName("v")] public decimal V { get; set; }
        [JsonPropertyName("t")] public long    T { get; set; }
    }
}