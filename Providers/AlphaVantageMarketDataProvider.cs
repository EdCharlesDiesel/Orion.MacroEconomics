using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Orion.MacroEconomics.Configuration;
using Orion.MacroEconomics.Entities;
using Orion.MacroEconomics.Providers.Interfaces;

namespace Orion.MacroEconomics.Providers;

public sealed class AlphaVantageMarketDataProvider(
    HttpClient httpClient,
    IOptions<AlphaVantageOptions> options,
    ILogger<AlphaVantageMarketDataProvider> logger) : IAlphaVantageMarketDataProvider
{
    private readonly AlphaVantageOptions _options = options.Value;

    public async Task<IReadOnlyList<OhlcvBar>> GetDailyFxCandlesAsync(
        string pair,
        CancellationToken cancellationToken = default)
    {
        var clean = NormalizePair(pair);
        var from = clean[..3];
        var to = clean[3..6];

        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException("Alpha Vantage API key is missing.");

        var url =
            $"/query?function=FX_DAILY" +
            $"&from_symbol={from}" +
            $"&to_symbol={to}" +
            $"&outputsize=full" +
            $"&apikey={_options.ApiKey}";

        logger.LogInformation("Fetching Alpha Vantage FX_DAILY for {Pair}", clean);

        using var response = await httpClient.GetAsync(url, cancellationToken);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Alpha Vantage failed: {response.StatusCode}");

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        if (doc.RootElement.TryGetProperty("Note", out var note))
            throw new InvalidOperationException(note.GetString());

        if (doc.RootElement.TryGetProperty("Error Message", out var error))
            throw new InvalidOperationException(error.GetString());

        if (!doc.RootElement.TryGetProperty("Time Series FX (Daily)", out var series))
            throw new InvalidOperationException("Alpha Vantage response did not contain FX daily series.");

        var candles = new List<OhlcvBar>();

        foreach (var item in series.EnumerateObject())
        {
            var date = DateTime.SpecifyKind(
                DateTime.Parse(item.Name, CultureInfo.InvariantCulture),
                DateTimeKind.Utc);

            var value = item.Value;

            candles.Add(new OhlcvBar
            {
                Pair = clean,
                TimestampUtc = date,
                Open = ParseDecimal(value, "1. open"),
                High = ParseDecimal(value, "2. high"),
                Low = ParseDecimal(value, "3. low"),
                Close = ParseDecimal(value, "4. close"),
                Volume = 0
            });
        }

        return candles.OrderBy(x => x.TimestampUtc).ToList();
    }

    private static decimal ParseDecimal(JsonElement element, string property)
    {
        return decimal.Parse(
            element.GetProperty(property).GetString()!,
            CultureInfo.InvariantCulture);
    }

    private static string NormalizePair(string pair)
    {
        var clean = pair.Trim().ToUpperInvariant()
            .Replace("/", "")
            .Replace("-", "")
            .Replace("=X", "");

        if (clean.Length != 6)
            throw new ArgumentException("Pair must be like EURUSD or EUR/USD.");

        return clean;
    }
}