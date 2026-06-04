using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Orion.MacroEconomics.Models;

namespace Orion.MacroEconomics.Services;

public class YahooFinanceService
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly TechnicalIndicatorService _indicatorService;
    private readonly ILogger<YahooFinanceService> _logger;

    public YahooFinanceService(HttpClient httpClient, IMemoryCache cache,
        TechnicalIndicatorService indicatorService, ILogger<YahooFinanceService> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _indicatorService = indicatorService;
        _logger = logger;
    }

    public async Task<List<MarketData>> FetchMarketDataAsync(
        string ticker, string interval, string period, string? resample = null)
    {
        var cacheKey = $"marketdata_{ticker}_{interval}_{period}_{resample}";

        if (_cache.TryGetValue(cacheKey, out List<MarketData>? cached))
        {
            return cached ?? new List<MarketData>();
        }

        try
        {
            // Yahoo Finance v8 API endpoint
            var url = $"https://query1.finance.yahoo.com/v8/finance/chart/{ticker}?" +
                      $"interval={interval}&range={period}";

            var response = await _httpClient.GetStringAsync(url);
            var data = ParseYahooResponse(response);

            if (resample != null)
            {
                data = ResampleData(data, resample);
            }

            // Calculate indicators
            var closes = data.Select(d => d.Close).ToList();
            var highs = data.Select(d => d.High).ToList();
            var lows = data.Select(d => d.Low).ToList();

            var ema50 = _indicatorService.CalculateEma(closes, 50);
            var ema200 = _indicatorService.CalculateEma(closes, 200);
            var rsi = _indicatorService.CalculateRsi(closes);
            var (macd, macdSig, macdHist) = _indicatorService.CalculateMacd(closes);
            var (adx, plusDi, minusDi) = _indicatorService.CalculateAdx(highs, lows, closes);

            // Align all indicators to the same length
            int minLength = new[] {
                data.Count, ema50.Count, ema200.Count,
                rsi.Count, macd.Count, adx.Count
            }.Min();

            var result = new List<MarketData>();
            for (int i = 0; i < minLength; i++)
            {
                result.Add(new MarketData
                {
                    Date = data[i].Date,
                    Open = data[i].Open,
                    High = data[i].High,
                    Low = data[i].Low,
                    Close = data[i].Close,
                    Volume = data[i].Volume,
                    Ema50 = ema50[i],
                    Ema200 = ema200[i],
                    Rsi = rsi[i],
                    Macd = macd[i],
                    MacdSig = macdSig[i],
                    MacdHist = macdHist[i],
                    Adx = adx[i],
                    PlusDi = plusDi[i],
                    MinusDi = minusDi[i]
                });
            }

            // Cache for 5 minutes
            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching data for {Ticker}", ticker);
            return new List<MarketData>();
        }
    }

    public async Task<AtrData?> FetchAtrAsync(string ticker, double pipSize)
    {
        var cacheKey = $"atr_{ticker}";

        if (_cache.TryGetValue(cacheKey, out AtrData? cached))
        {
            return cached;
        }

        try
        {
            var data = await FetchMarketDataAsync(ticker, "1d", "60d");

            if (data.Count < 22) return null;

            var high = data.Select(d => d.High).ToList();
            var low = data.Select(d => d.Low).ToList();
            var close = data.Select(d => d.Close).ToList();

            var trValues = new List<double>();
            for (int i = 1; i < data.Count; i++)
            {
                double tr1 = high[i] - low[i];
                double tr2 = Math.Abs(high[i] - close[i - 1]);
                double tr3 = Math.Abs(low[i] - close[i - 1]);
                trValues.Add(Math.Max(Math.Max(tr1, tr2), tr3));
            }

            var atr14Values = _indicatorService.CalculateEma(trValues, 14);
            var atr20Values = _indicatorService.CalculateEma(trValues, 20);

            double a14 = atr14Values.Last();
            double a20 = atr20Values.Last();
            double a14Pips = Math.Round(a14 / pipSize, 1);
            double a20Pips = Math.Round(a20 / pipSize, 1);
            double slPips = Math.Round(a14Pips * 1.5, 1);

            var result = new AtrData
            {
                Atr14 = Math.Round(a14, 5),
                Atr20 = Math.Round(a20, 5),
                Atr14Pips = a14Pips,
                Atr20Pips = a20Pips,
                SlPips = slPips,
                Tp1Pips = Math.Round(slPips * 2.0, 1),
                Tp2Pips = Math.Round(slPips * 3.0, 1),
                AtrOk = a14 > a20
            };

            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching ATR for {Ticker}", ticker);
            return null;
        }
    }

    private List<MarketData> ParseYahooResponse(string jsonResponse)
    {
        var result = new List<MarketData>();

        using var doc = JsonDocument.Parse(jsonResponse);
        var root = doc.RootElement;

        if (!root.TryGetProperty("chart", out var chart) ||
            !chart.TryGetProperty("result", out var results) ||
            results.GetArrayLength() == 0)
        {
            return result;
        }

        var resultData = results[0];
        var timestamps = resultData.GetProperty("timestamp").EnumerateArray()
            .Select(t => DateTimeOffset.FromUnixTimeSeconds(t.GetInt64()).DateTime)
            .ToList();

        var quotes = resultData.GetProperty("indicators").GetProperty("quote")[0];
        var opens = quotes.GetProperty("open").EnumerateArray()
            .Select(v => v.ValueKind == JsonValueKind.Null ? 0 : v.GetDouble()).ToList();
        var highs = quotes.GetProperty("high").EnumerateArray()
            .Select(v => v.ValueKind == JsonValueKind.Null ? 0 : v.GetDouble()).ToList();
        var lows = quotes.GetProperty("low").EnumerateArray()
            .Select(v => v.ValueKind == JsonValueKind.Null ? 0 : v.GetDouble()).ToList();
        var closes = quotes.GetProperty("close").EnumerateArray()
            .Select(v => v.ValueKind == JsonValueKind.Null ? 0 : v.GetDouble()).ToList();
        var volumes = quotes.GetProperty("volume").EnumerateArray()
            .Select(v => v.ValueKind == JsonValueKind.Null ? 0 : v.GetInt64()).ToList();

        for (int i = 0; i < timestamps.Count; i++)
        {
            if (opens[i] == 0 || highs[i] == 0 || lows[i] == 0 || closes[i] == 0)
                continue;

            result.Add(new MarketData
            {
                Date = timestamps[i],
                Open = opens[i],
                High = highs[i],
                Low = lows[i],
                Close = closes[i],
                Volume = volumes[i]
            });
        }

        return result;
    }

    private List<MarketData> ResampleData(List<MarketData> data, string resample)
    {
        if (data.Count == 0) return data;

        var grouped = data.GroupBy(d => GetResampleKey(d.Date, resample))
            .Select(g => new MarketData
            {
                Date = g.Key,
                Open = g.First().Open,
                High = g.Max(d => d.High),
                Low = g.Min(d => d.Low),
                Close = g.Last().Close,
                Volume = g.Sum(d => d.Volume)
            })
            .OrderBy(d => d.Date)
            .ToList();

        return grouped;
    }

    private DateTime GetResampleKey(DateTime date, string resample)
    {
        return resample switch
        {
            "1h" => new DateTime(date.Year, date.Month, date.Day, date.Hour, 0, 0),
            "4h" => new DateTime(date.Year, date.Month, date.Day, (date.Hour / 4) * 4, 0, 0),
            "1W" => date.AddDays(-(int)date.DayOfWeek).Date,
            _ => date.Date
        };
    }
}