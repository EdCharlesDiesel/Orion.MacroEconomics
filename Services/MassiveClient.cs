using System.Net;
using System.Text.Json;
using Orion.MacroEconomics.Models;

namespace Orion.MacroEconomics.Services;

/// <summary>
/// Typed HTTP client for the Massive Forex REST API.
/// Covers every endpoint documented at:
/// https://massive.com/docs/rest/forex/overview
/// </summary>
public sealed class MassiveClient(HttpClient http, ILogger<MassiveClient> log)
{
    private static readonly SemaphoreSlim _throttle = new(3, 3);

    private static readonly JsonSerializerOptions _json = new() { PropertyNameCaseInsensitive = true };

    // ── Tickers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// GET /v3/reference/tickers — all forex tickers.
    /// </summary>
    public async Task<List<ForexTicker>> GetAllTickersAsync(CancellationToken ct = default, int limit = 1000)
    {
        var url = $"/v3/reference/tickers?market=fx&active=true&limit={limit}";
        var response = await GetAsync<MassiveResponse<List<ForexTicker>>>(url, ct);
        return response?.Results ?? [];
    }

    /// <summary>
    /// GET /v3/reference/tickers/{ticker} — details for one ticker.
    /// </summary>
    public async Task<TickerDetails?> GetTickerDetailsAsync(string ticker, CancellationToken ct = default)
    {
        var url = $"/v3/reference/tickers/{Uri.EscapeDataString(ticker)}";
        var response = await GetAsync<MassiveResponse<TickerDetails>>(url, ct);
        return response?.Results;
    }


    /// <summary>
    /// GET /v1/conversion/{from}/{to} — real-time conversion rate.
    /// </summary>
    public async Task<ConversionResult?> GetConversionAsync(string from, string to, decimal amount = 1,
        CancellationToken ct = default)
    {
        var url = $"/v1/conversion/{Uri.EscapeDataString(from)}/{Uri.EscapeDataString(to)}" +
                  $"?amount={amount}&precision=2";
        return await GetAsync<ConversionResult>(url, ct);
    }


    /// <summary>
    /// GET /v2/aggs/ticker/{forexTicker}/range/{multiplier}/{timespan}/{from}/{to}
    /// Custom OHLC bars for a date range.
    /// timespan: minute | hour | day | week | month | quarter | year
    /// </summary>
    public async Task<AggResponse?> GetCustomBarsAsync(string ticker, int multiplier, string timespan, DateOnly from,
        DateOnly to, bool adjusted = true, string sort = "asc", int limit = 50000, CancellationToken ct = default)
    {
        var url = $"/v2/aggs/ticker/{Uri.EscapeDataString(ticker)}" +
                  $"/range/{multiplier}/{timespan}" +
                  $"/{from:yyyy-MM-dd}/{to:yyyy-MM-dd}" +
                  $"?adjusted={adjusted.ToString().ToLower()}&sort={sort}&limit={limit}";
        return await GetAsync<AggResponse>(url, ct);
    }

    /// <summary>
    /// GET /v2/aggs/grouped/locale/global/market/fx/{date}
    /// Daily OHLC for ALL forex tickers on a given date.
    /// </summary>
    public async Task<AggResponse?> GetDailyMarketSummaryAsync(DateOnly date, bool adjusted = true,
        CancellationToken ct = default)
    {
        var url = $"/v2/aggs/grouped/locale/global/market/fx/{date:yyyy-MM-dd}" +
                  $"?adjusted={adjusted.ToString().ToLower()}";
        return await GetAsync<AggResponse>(url, ct);
    }

    /// <summary>
    /// GET /v2/aggs/ticker/{forexTicker}/prev
    /// Previous trading day's OHLC bar.
    /// </summary>
    public async Task<AggResponse?> GetPreviousDayBarAsync(string ticker, bool adjusted = true,
        CancellationToken ct = default)
    {
        var url = $"/v2/aggs/ticker/{Uri.EscapeDataString(ticker)}/prev" +
                  $"?adjusted={adjusted.ToString().ToLower()}";
        return await GetAsync<AggResponse>(url, ct);
    }


    /// <summary>
    /// GET /v2/snapshot/locale/global/markets/forex/tickers/{ticker}
    /// Snapshot for a single ticker.
    /// </summary>
    public async Task<ForexSnapshot?> GetSnapshotAsync(string ticker, CancellationToken ct = default)
    {
        var url = $"/v2/snapshot/locale/global/markets/forex/tickers/{Uri.EscapeDataString(ticker)}";
        var response = await GetAsync<SnapshotResponse>(url, ct);
        return response?.Tickers?.FirstOrDefault();
    }

    /// <summary>
    /// GET /v2/snapshot/locale/global/markets/forex/tickers
    /// Full market snapshot for all (or specified) tickers.
    /// </summary>
    public async Task<List<ForexSnapshot>> GetFullMarketSnapshotAsync(IEnumerable<string>? tickers = null,
        CancellationToken ct = default)
    {
        var url = "/v2/snapshot/locale/global/markets/forex/tickers";
        if (tickers is not null)
        {
            var joined = string.Join(",", tickers.Select(Uri.EscapeDataString));
            if (!string.IsNullOrWhiteSpace(joined))
                url += $"?tickers={joined}";
        }

        var response = await GetAsync<SnapshotResponse>(url, ct);
        return response?.Tickers ?? [];
    }

    /// <summary>
    /// GET /v3/snapshot
    /// Unified snapshot across asset classes.
    /// </summary>
    public async Task<List<ForexSnapshot>> GetUnifiedSnapshotAsync(IEnumerable<string> tickers,
        CancellationToken ct = default)
    {
        var joined = string.Join(",", tickers.Select(Uri.EscapeDataString));
        var url = $"/v3/snapshot?ticker.any_of={joined}";
        var response = await GetAsync<SnapshotResponse>(url, ct);
        return response?.Tickers ?? [];
    }

    /// <summary>
    /// GET /v2/snapshot/locale/global/markets/forex/{direction}
    /// Top 20 gainers or losers.
    /// direction: gainers | losers
    /// </summary>
    public async Task<List<ForexSnapshot>> GetTopMoversAsync(string direction = "gainers",
        CancellationToken ct = default)
    {
        var url = $"/v2/snapshot/locale/global/markets/forex/{direction}";
        var response = await GetAsync<SnapshotResponse>(url, ct);
        return response?.Tickers ?? [];
    }


    /// <summary>
    /// GET /v3/quotes/{fxTicker}
    /// Historical BBO quotes for a forex pair.
    /// </summary>
    public async Task<List<ForexQuote>> GetHistoricalQuotesAsync(string ticker, DateTime? from = null,
        DateTime? to = null, int limit = 1000, CancellationToken ct = default)
    {
        var url = $"/v3/quotes/{Uri.EscapeDataString(ticker)}?limit={limit}&order=desc";
        if (from.HasValue)
            url += $"&timestamp.gte={from.Value:yyyy-MM-ddTHH:mm:ssZ}";
        if (to.HasValue)
            url += $"&timestamp.lte={to.Value:yyyy-MM-ddTHH:mm:ssZ}";

        var response = await GetAsync<MassiveResponse<List<ForexQuote>>>(url, ct);
        return response?.Results ?? [];
    }

    /// <summary>
    /// GET /v1/last_quote/currencies/{from}/{to}
    /// Most recent quote for a currency pair.
    /// </summary>
    public async Task<LastQuoteResult?> GetLastQuoteAsync(string from, string to, CancellationToken ct = default)
    {
        var url = $"/v1/last_quote/currencies/{Uri.EscapeDataString(from)}/{Uri.EscapeDataString(to)}";
        var response = await GetAsync<MassiveResponse<LastQuoteResult>>(url, ct);
        return response?.Results;
    }

    /// <summary>
    /// GET /v1/indicators/sma/{fxTicker}
    /// Simple Moving Average.
    /// </summary>
    public async Task<IndicatorResponse?> GetSmaAsync(string ticker, string timespan = "day", int window = 20, string seriesType = "close", int limit = 10, CancellationToken ct = default)
    {
        var url = $"/v1/indicators/sma/{Uri.EscapeDataString(ticker)}" +
                  $"?timespan={timespan}&window={window}&series_type={seriesType}&limit={limit}&order=desc";
        return await GetAsync<IndicatorResponse>(url, ct);
    }

    /// <summary>
    /// GET /v1/indicators/ema/{fxTicker}
    /// Exponential Moving Average.
    /// </summary>
    public async Task<IndicatorResponse?> GetEmaAsync(string ticker, string timespan = "day", int window = 20,
        string seriesType = "close", int limit = 10, CancellationToken ct = default)
    {
        var url = $"/v1/indicators/ema/{Uri.EscapeDataString(ticker)}" +
                  $"?timespan={timespan}&window={window}&series_type={seriesType}&limit={limit}&order=desc";
        return await GetAsync<IndicatorResponse>(url, ct);
    }

    /// <summary>
    /// GET /v1/indicators/macd/{fxTicker}
    /// Moving Average Convergence/Divergence.
    /// </summary>
    public async Task<MacdResponse?> GetMacdAsync(string ticker, string timespan = "day", int shortWindow = 12, int longWindow = 26, int signalWindow = 9, string seriesType = "close", int limit = 10, CancellationToken ct = default)
    {
        var url = $"/v1/indicators/macd/{Uri.EscapeDataString(ticker)}" +
                  $"?timespan={timespan}" +
                  $"&short_window={shortWindow}&long_window={longWindow}&signal_window={signalWindow}" +
                  $"&series_type={seriesType}&limit={limit}&order=desc";
        return await GetAsync<MacdResponse>(url, ct);
    }

    /// <summary>
    /// GET /v1/indicators/rsi/{fxTicker}
    /// Relative Strength Index.
    /// </summary>
    public async Task<IndicatorResponse?> GetRsiAsync(string ticker, string timespan = "day", int window = 14,
        string seriesType = "close", int limit = 10, CancellationToken ct = default)
    {
        var url = $"/v1/indicators/rsi/{Uri.EscapeDataString(ticker)}" +
                  $"?timespan={timespan}&window={window}&series_type={seriesType}&limit={limit}&order=desc";
        return await GetAsync<IndicatorResponse>(url, ct);
    }

    /// <summary>
    /// GET /v3/reference/exchanges — list of known exchanges.
    /// </summary>
    public async Task<List<Exchange>> GetExchangesAsync(string assetClass = "fx", CancellationToken ct = default)
    {
        var url = $"/v3/reference/exchanges?asset_class={assetClass}";
        var response = await GetAsync<MassiveResponse<List<Exchange>>>(url, ct);
        return response?.Results ?? [];
    }

    /// <summary>
    /// GET /v1/marketstatus/upcoming — upcoming market holidays.
    /// </summary>
    public async Task<List<MarketHoliday>> GetMarketHolidaysAsync(CancellationToken ct = default)
    {
        var url = "/v1/marketstatus/upcoming";
        return await GetAsync<List<MarketHoliday>>(url, ct) ?? [];
    }

    /// <summary>
    /// GET /v1/marketstatus/now — current market open/close status.
    /// </summary>
    public async Task<MarketStatus?> GetMarketStatusAsync(CancellationToken ct = default)
    {
        return await GetAsync<MarketStatus>("/v1/marketstatus/now", ct);
    }

    private async Task<T?> GetAsync<T>(string url, CancellationToken ct, int maxRetries = 4)
    {
        await _throttle.WaitAsync(ct);
        try
        {
            for (var attempt = 1; attempt <= maxRetries; attempt++)
            {
                HttpResponseMessage resp;
                try
                {
                    resp = await http.GetAsync(url, ct);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    log.LogWarning(ex, "HTTP error on attempt {Attempt} for {Url}", attempt, url);
                    if (attempt == maxRetries) return default;
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), ct);
                    continue;
                }

                if (resp.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    if (attempt == maxRetries)
                    {
                        log.LogError("Rate limit not resolved after {Max} retries: {Url}", maxRetries, url);
                        return default;
                    }

                    var wait = resp.Headers.RetryAfter?.Delta
                               ?? TimeSpan.FromSeconds(Math.Pow(2, attempt));

                    log.LogWarning("Rate limited (429). Waiting {Wait:F1}s — attempt {A}/{M}",
                        wait.TotalSeconds, attempt, maxRetries);

                    await Task.Delay(wait, ct);
                    continue;
                }

                if (!resp.IsSuccessStatusCode)
                {
                    log.LogWarning("HTTP {Code} for {Url}", (int)resp.StatusCode, url);
                    return default;
                }

                var body = await resp.Content.ReadAsStringAsync(ct);
                await Task.Delay(200, ct); // polite gap between requests
                return JsonSerializer.Deserialize<T>(body, _json);
            }

            return default;
        }
        finally
        {
            _throttle.Release();
        }
    }
}