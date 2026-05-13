using System.Text.Json;
using Microsoft.Extensions.Options;
using Orion.MacroEconomics.Configuration;
using Orion.MacroEconomics.DTO;
using Orion.MacroEconomics.Entities;
using Orion.MacroEconomics.Providers.Interfaces;

namespace Orion.MacroEconomics.Providers
{
    public sealed class TradingEconomicsProvider(
        IHttpClientFactory httpClientFactory,
        HttpClient httpClient,
        IConfiguration configuration,
        IOptions<TradingEconomicsOptions> options,
        ILogger<TradingEconomicsProvider> logger) : ITradingEconomicsProvider
    {
        private readonly TradingEconomicsOptions _options = options.Value;

        private static readonly Dictionary<string, string> CurrencyToCountry = new()
        {
            ["USD"] = "United States",
            ["EUR"] = "Euro Area",
            ["GBP"] = "United Kingdom",
            ["JPY"] = "Japan",
            ["ZAR"] = "South Africa",
            ["AUD"] = "Australia",
            ["NZD"] = "New Zealand",
            ["CAD"] = "Canada",
            ["CHF"] = "Switzerland"
        };

        private static readonly string[] Indicators =
        {
            "GDP Growth Rate",
            "Inflation Rate",
            "Interest Rate",
            "Unemployment Rate"
        };

        public async Task<MacroData> GetMacroDataAsync(
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(_options.ApiKey))
                throw new InvalidOperationException("Trading Economics API key is missing.");

            var result = new Dictionary<string, CurrencyMacroData>();

            foreach (var item in CurrencyToCountry)
            {
                var currency = item.Key;
                var country = item.Value;

                var values = await FetchCountryIndicatorsAsync(country, cancellationToken);

                result[currency] = new CurrencyMacroData
                {
                    GDP = Get(values, "GDP Growth Rate"),
                    Inflation = Get(values, "Inflation Rate"),
                    Rates = Get(values, "Interest Rate"),
                    Unemployment = Get(values, "Unemployment Rate"),
                    LastUpdated = DateTime.UtcNow,
                    IsLiveData = true
                };
            }

            return new MacroData
            {
                Data = result,
                IsLive = true,
                LastUpdated = DateTime.UtcNow,
                Warning = null,
                DataSource = "Trading Economics API"
            };
        }

        public Dictionary<string, Dictionary<string, string>> GetFredSeriesMappings()
        {
            throw new NotImplementedException();
        }

        public Task<FredStatusResponse> CheckStatusAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        private async Task<Dictionary<string, decimal>> FetchCountryIndicatorsAsync(
            string country,
            CancellationToken cancellationToken)
        {
            var output = new Dictionary<string, decimal>();

            foreach (var indicator in Indicators)
            {
                try
                {
                    var value = await FetchLatestIndicatorValueAsync(
                        country,
                        indicator,
                        cancellationToken);

                    if (value.HasValue)
                        output[indicator] = value.Value;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(
                        ex,
                        "Failed to fetch {Indicator} for {Country}",
                        indicator,
                        country);
                }
            }

            return output;
        }

        private async Task<decimal?> FetchLatestIndicatorValueAsync(
            string country,
            string indicator,
            CancellationToken cancellationToken)
        {
            var client = httpClientFactory.CreateClient("TradingEconomics");

            var encodedCountry = Uri.EscapeDataString(country);
            var encodedIndicator = Uri.EscapeDataString(indicator);

            var url =
                $"/historical/country/{encodedCountry}/indicator/{encodedIndicator}?c={_options.ApiKey}";

            using var response = await client.GetAsync(url, cancellationToken);

            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync(cancellationToken);

            using var doc = JsonDocument.Parse(json);

            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                return null;

            var latest = doc.RootElement
                .EnumerateArray()
                .Where(x => x.TryGetProperty("DateTime", out _))
                .OrderByDescending(x =>
                {
                    var dateText = x.GetProperty("DateTime").GetString();
                    return DateTime.TryParse(dateText, out var date)
                        ? date
                        : DateTime.MinValue;
                })
                .FirstOrDefault();

            if (latest.ValueKind == JsonValueKind.Undefined)
                return null;

            if (latest.TryGetProperty("Close", out var closeElement) &&
                closeElement.TryGetDecimal(out var close))
            {
                return close;
            }

            if (latest.TryGetProperty("Value", out var valueElement) &&
                valueElement.TryGetDecimal(out var value))
            {
                return value;
            }

            return null;
        }

        private static decimal Get(Dictionary<string, decimal> values, string key)
        {
            return values.TryGetValue(key, out var value) ? value : 0m;
        }
        
        public async Task<object> GetAsync(string symbol, DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken = default)
        {
            var apiKey = configuration["TradingEconomics:ApiKey"];

            if (string.IsNullOrWhiteSpace(apiKey))
                throw new InvalidOperationException("Trading Economics API key is missing.");

            var url =
                $"calendar/country/{symbol}" +
                $"?c={apiKey}" +
                $"&f=json" +
                $"&d1={fromUtc:yyyy-MM-dd}" +
                $"&d2={toUtc:yyyy-MM-dd}";

            var result = await httpClient.GetFromJsonAsync<object>(url, cancellationToken);

            return result ?? Array.Empty<object>();
        }

        public string Name { get; }
        public bool CanHandle(MarketDataRequest request)
        {
            throw new NotImplementedException();
        }

        public Task<ProviderDataResult> FetchAsync(MarketDataRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyList<OhlcvBar>> GetHistoricalCandlesAsync(MarketDataRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<MarketQuote?> GetLatestQuoteAsync(string pair, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<MarketTick?> GetLatestTickAsync(string pair, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<MarketDataHealth> CheckHealthAsync(string pair, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}