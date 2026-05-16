using System.Text.Json;
using System.Text.Json.Serialization;
using Orion.MacroEconomics.Configurations;
using Orion.MacroEconomics.Entities;
using Orion.MacroEconomics.Models;
using Orion.MacroEconomics.Providers.Interfaces;

namespace Orion.MacroEconomics.Providers;

public class MassiveDataProvider(HttpClient http, ILogger<MassiveDataProvider> log): IMassiveDataProvider
{
    private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };
    

    /// <summary>
    /// Fetch OHLCV candles for a pair/timeframe combination.
    /// pair      = "EUR/USD" (matches AppConfig.Assets key)
    /// timeframe = "Daily"   (matches AppConfig.Timeframes key)
    /// </summary>
    public async Task<List<Candle>> GetCandlesAsync(string pair, string timeframe, CancellationToken ct = default)
    {
        if (!AppConfig.Assets.TryGetValue(pair, out var ticker))
        {
            log.LogWarning("No Massive ticker for pair '{Pair}'", pair);
            return [];
        }

        if (!AppConfig.Timeframes.TryGetValue(timeframe, out var tf))
        {
            log.LogWarning("Unknown timeframe '{Tf}'", timeframe);
            return [];
        }

        var (mult, span, days) = tf;
        var to   = DateOnly.FromDateTime(DateTime.UtcNow);
        var from = to.AddDays(-days);
        var url  = $"/v2/aggs/ticker/{ticker}/range/{mult}/{span}/{from:yyyy-MM-dd}/{to:yyyy-MM-dd}"
                 + "?adjusted=true&sort=asc&limit=50000";

        try
        {
            var resp = await http.GetAsync(url, ct);
            resp.EnsureSuccessStatusCode();
            var body = await resp.Content.ReadAsStringAsync(ct);
            var agg  = JsonSerializer.Deserialize<MassiveAggResponse>(body, _json);
            if (agg?.Results is null || agg.Results.Count == 0)
            {
                log.LogWarning("No bars returned for {Pair}/{Tf}", pair, timeframe);
                return [];
            }
            return agg.Results.Select(r => new Candle
            {
                Time   = DateTimeOffset.FromUnixTimeMilliseconds(r.T).UtcDateTime,
                Open   = r.O,
                High   = r.H,
                Low    = r.L,
                Close  = r.C,
                Volume = r.V,
            }).ToList();
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Massive fetch failed for {Pair}/{Tf}", pair, timeframe);
            return [];
        }
    }

 
    public async Task<(decimal Price, decimal ChangePercent)> GetSnapshotAsync(string pair, CancellationToken ct = default)
    {
        var candles = await GetCandlesAsync(pair, "Daily", ct);
        if (candles.Count < 2)
            return (0, 0);
        var price  = (decimal)candles[^1].Close;
        var prev   = (decimal)candles[^2].Close;
        var change = prev > 0 ? (price - prev) / prev * 100 : 0;
        return (price, change);
    }
        
    public static readonly Dictionary<string, MacroSnapshot> Fallbacks = new()
    {
        { "USD", new() { Currency = "USD", GDP = 2.5,  Inflation = 3.2, Rates = 5.50m, Unemployment = 3.8m  } },
        { "EUR", new() { Currency = "EUR", GDP = 0.8,  Inflation = 2.9, Rates = 4.50m, Unemployment = 6.5m  } },
        { "GBP", new() { Currency = "GBP", GDP = 0.6,  Inflation = 3.4, Rates = 5.25m, Unemployment = 4.2m  } },
        { "JPY", new() { Currency = "JPY", GDP = 1.1,  Inflation = 2.8, Rates = -0.10m,Unemployment = 2.6m  } },
        { "ZAR", new() { Currency = "ZAR", GDP = 1.2,  Inflation = 5.0, Rates = 8.25m, Unemployment = 32.1m } },
        { "AUD", new() { Currency = "AUD", GDP = 2.0,  Inflation = 4.1, Rates = 4.35m, Unemployment = 3.9m  } },
        { "NZD", new() { Currency = "NZD", GDP = 2.2,  Inflation = 3.8, Rates = 5.50m, Unemployment = 3.9m  } },
        { "CAD", new() { Currency = "CAD", GDP = 1.5,  Inflation = 3.4, Rates = 5.00m, Unemployment = 5.1m  } },
        { "CHF", new() { Currency = "CHF", GDP = 0.9,  Inflation = 2.1, Rates = 1.75m, Unemployment = 2.0m  } },
    };

    /// <summary>
    /// 
    /// </summary>
    /// <param name="ct"></param>
    /// <returns></returns>
    public async Task<(List<MacroSnapshot> Data, bool IsLive)> GetAllMacroAsync(CancellationToken ct = default)
    {
        var result  = Fallbacks.Values.Select(v => Clone(v)).ToList();
        var isLive  = false;

        try
        {
            var gdp  = await GetLatestIndicatorAsync("gdp",                "quarterly", ct);
            var cpi  = await GetLatestIndicatorAsync("cpi",                "monthly",   ct);
            var rate = await GetLatestIndicatorAsync("federal_funds_rate", "monthly",   ct);
            var unem = await GetLatestIndicatorAsync("unemployment",       "monthly",   ct);

            if (gdp.HasValue || cpi.HasValue || rate.HasValue || unem.HasValue)
            {
                var usd = result.First(r => r.Currency == "USD");
                if (gdp.HasValue)  usd.GDP          = Math.Round(gdp.Value,  2);
                if (cpi.HasValue)  usd.Inflation    = Math.Round(cpi.Value,  2);
                if (rate.HasValue) usd.Rates        = (decimal)Math.Round(rate.Value, 2);
                if (unem.HasValue) usd.Unemployment = (decimal)Math.Round(unem.Value, 2);
                isLive = true;
            }
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Live macro fetch failed — using fallback values");
        }

        return (result, isLive);
    }

    public Task<List<OhlcvBar>> FetchDataAsync(string pair, string s, string s1, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public async Task<List<EconomyDataPoint>> GetSeriesAsync(string seriesId, int limit = 36, CancellationToken ct = default)
    {
        try
        {
            return seriesId switch
            {
                "FEDFUNDS"        => await GetIndicatorSeriesAsync("federal_funds_rate", "monthly",    limit, ct),
                "CPIAUCSL"        => await GetIndicatorSeriesAsync("cpi",                "monthly",    limit, ct),
                "UNRATE"          => await GetIndicatorSeriesAsync("unemployment",       "monthly",    limit, ct),
                "A191RL1Q225SBEA" => await GetIndicatorSeriesAsync("gdp",                "quarterly",  limit, ct),
                "DGS10"           => await GetIndicatorSeriesAsync("treasury_yield",     "daily",      limit, ct, "10Y"),
                "DGS2"            => await GetIndicatorSeriesAsync("treasury_yield",     "daily",      limit, ct, "2Y"),
                "DTWEXBGS"        => await GetIndexSeriesAsync("I:DXY", limit, ct),
                "VIXCLS"          => await GetIndexSeriesAsync("I:VIX", limit, ct),
                _ => []
            };
        }
        catch (Exception ex)
        {
            log.LogWarning(ex, "Economy series '{Id}' failed", seriesId);
            return [];
        }
    }

    private async Task<double?> GetLatestIndicatorAsync(string slug, string timespan, CancellationToken ct)
    {
        var url  = $"/v1/indicatordata/{slug}?timespan={timespan}&limit=1";
        var resp = await http.GetAsync(url, ct);
        resp.EnsureSuccessStatusCode();
        var body   = await resp.Content.ReadAsStringAsync(ct);
        var parsed = JsonSerializer.Deserialize<IndicatorResponse>(body, _json);
        var data   = parsed?.Results?.Data;
        if (data is { Count: > 0 })
            return data[^1].Value;
        return null;
    }

    private async Task<List<EconomyDataPoint>> GetIndicatorSeriesAsync(string slug, string timespan, int limit, CancellationToken ct, string? yieldCurve = null)
    {
        var query = $"?timespan={timespan}&limit={limit}";
        if (yieldCurve is not null) query += $"&yield_curve={yieldCurve}";
        var url    = $"/v1/indicatordata/{slug}{query}";
        var resp   = await http.GetAsync(url, ct);
        resp.EnsureSuccessStatusCode();
        var body   = await resp.Content.ReadAsStringAsync(ct);
        var parsed = JsonSerializer.Deserialize<IndicatorResponse>(body, _json);
        return parsed?.Results?.Data?
            .Where(d => d.Value.HasValue)
            .Select(d => new EconomyDataPoint { Date = d.Date, Value = (decimal)d.Value!.Value })
            .OrderBy(d => d.Date)
            .ToList() ?? [];
    }

    private async Task<List<EconomyDataPoint>> GetIndexSeriesAsync(string ticker, int limit, CancellationToken ct)
    {
        var to   = DateOnly.FromDateTime(DateTime.UtcNow);
        var from = to.AddDays(-limit * 2);
        var url  = $"/v2/aggs/ticker/{ticker}/range/1/day/{from:yyyy-MM-dd}/{to:yyyy-MM-dd}?sort=asc&limit={limit}";
        var resp = await http.GetAsync(url, ct);
        resp.EnsureSuccessStatusCode();
        var body   = await resp.Content.ReadAsStringAsync(ct);
        var parsed = JsonSerializer.Deserialize<MassiveAggResponse>(body, _json);
        return parsed?.Results?
            .Select(r => new EconomyDataPoint
            {
                Date  = DateTimeOffset.FromUnixTimeMilliseconds(r.T).UtcDateTime.Date,
                Value = (decimal)r.C,
            })
            .ToList() ?? [];
    }

    private static MacroSnapshot Clone(MacroSnapshot s) => new()
    {
        Currency = s.Currency, GDP = s.GDP, Inflation = s.Inflation,
        Rates = s.Rates, Unemployment = s.Unemployment,
    };

    private class IndicatorResponse
    {
        [JsonPropertyName("results")]
        public IndicatorResults? Results { get; set; }
    }
    private class IndicatorResults
    {
        [JsonPropertyName("data")]
        public List<DataPoint>? Data { get; set; }
    }
    private class DataPoint
    {
        [JsonPropertyName("date")]  public DateTime Date  { get; set; }
        [JsonPropertyName("value")] public double?  Value { get; set; }
    }
    private class MassiveAggResponse
    {
        [JsonPropertyName("results")] public List<AggBar>? Results { get; set; }
    }
    private class AggBar
    {
        [JsonPropertyName("c")] 
        public decimal C { get; set; }
        [JsonPropertyName("t")] public long   T { get; set; }
        public decimal L { get; set; }
        public decimal H { get; set; }
        public decimal V { get; set; }
        public decimal O { get; set; }
    }

}
