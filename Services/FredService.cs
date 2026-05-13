using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Orion.MacroEconomics.DTO;
using Orion.MacroEconomics.Engine;
using Orion.MacroEconomics.Entities;
using Orion.MacroEconomics.Interfaces;
using Polly;
using Polly.Extensions.Http;

namespace Orion.MacroEconomics.Services
{
    public class FredService : IFredService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _cache;
        private readonly ILogger<FredService> _logger;
        private readonly IConfiguration _configuration;
        private readonly FredServiceOptions _options;
        private readonly AuditTrailEngine? _auditTrail;

        private const string CACHE_KEY_PREFIX = "FRED_MACRO_DATA";

        private static readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();

        private static readonly Dictionary<string, Dictionary<string, FredSeriesInfo>> FredSeries = new()
        {
            ["USD"] = new()
            {
                ["GDP"] = new("A191RL1Q225SBEA", "Real GDP Growth Rate", "%", Frequency.Quarterly),
                ["CPI"] = new("CPIAUCSL", "Consumer Price Index", "Index", Frequency.Monthly),
                ["Rates"] = new("FEDFUNDS", "Federal Funds Rate", "%", Frequency.Monthly),
                ["Unemployment"] = new("UNRATE", "Unemployment Rate", "%", Frequency.Monthly),
                ["Debt"] = new("GFDEBTN", "Federal Debt", "Millions", Frequency.Quarterly),
                ["IndustrialProduction"] = new("INDPRO", "Industrial Production", "Index", Frequency.Monthly),
                ["RetailSales"] = new("RSXFS", "Retail Sales", "Millions", Frequency.Monthly),
                ["TradeBalance"] = new("BOPGSTB", "Trade Balance", "Millions", Frequency.Monthly)
            },
            ["EUR"] = new()
            {
                ["GDP"] = new("CLVMNACSCAB1GQEA19", "GDP Euro Area", "Index", Frequency.Quarterly),
                ["CPI"] = new("CP0000EZ19M086NEST", "CPI Euro Area", "Index", Frequency.Monthly),
                ["Rates"] = new("ECBDFR", "ECB Deposit Facility Rate", "%", Frequency.Monthly),
                ["Unemployment"] = new("LRHUTTTTEZM156S", "Unemployment Rate Euro Area", "%", Frequency.Monthly)
            },
            ["GBP"] = new()
            {
                ["GDP"] = new("CLVMNACSCAB1GQGB", "GDP UK", "Index", Frequency.Quarterly),
                ["CPI"] = new("GBRCPIALLMINMEI", "CPI UK", "Index", Frequency.Monthly),
                ["Rates"] = new("BOERUKM", "BOE Official Rate", "%", Frequency.Monthly),
                ["Unemployment"] = new("LRHUTTTTGBM156S", "Unemployment Rate UK", "%", Frequency.Monthly)
            },
            ["JPY"] = new()
            {
                ["GDP"] = new("JPNRGDPEXP", "GDP Japan", "Index", Frequency.Quarterly),
                ["CPI"] = new("JPNCPIALLMINMEI", "CPI Japan", "Index", Frequency.Monthly),
                ["Rates"] = new("IRSTCI01JPM156N", "Interest Rate Japan", "%", Frequency.Monthly),
                ["Unemployment"] = new("LRHUTTTTJPM156S", "Unemployment Rate Japan", "%", Frequency.Monthly)
            },
            ["ZAR"] = new()
            {
                ["GDP"] = new("ZAFGDPRQPSMEI", "GDP South Africa", "Index", Frequency.Quarterly),
                ["CPI"] = new("ZAFCPIALLMINMEI", "CPI South Africa", "Index", Frequency.Monthly),
                ["Rates"] = new("IRSTCI01ZAM156N", "Interest Rate South Africa", "%", Frequency.Monthly),
                ["Unemployment"] = new("LRHUTTTTZAM156S", "Unemployment Rate SA", "%", Frequency.Monthly)
            },
            ["AUD"] = new()
            {
                ["GDP"] = new("AUSGDPRQPSMEI", "GDP Australia", "Index", Frequency.Quarterly),
                ["CPI"] = new("AUSCPIALLMINMEI", "CPI Australia", "Index", Frequency.Monthly),
                ["Rates"] = new("IRSTCI01AUM156N", "Interest Rate Australia", "%", Frequency.Monthly),
                ["Unemployment"] = new("LRHUTTTTAUM156S", "Unemployment Rate Australia", "%", Frequency.Monthly)
            },
            ["NZD"] = new()
            {
                ["GDP"] = new("NZLGDPRQPSMEI", "GDP New Zealand", "Index", Frequency.Quarterly),
                ["CPI"] = new("NZLCPIALLMINMEI", "CPI New Zealand", "Index", Frequency.Monthly),
                ["Rates"] = new("IRSTCI01NZM156N", "Interest Rate New Zealand", "%", Frequency.Monthly),
                ["Unemployment"] = new("LRHUTTTTNZM156S", "Unemployment Rate NZ", "%", Frequency.Monthly)
            },
            ["CAD"] = new()
            {
                ["GDP"] = new("CANGDPRQPSMEI", "GDP Canada", "Index", Frequency.Quarterly),
                ["CPI"] = new("CANCPIALLMINMEI", "CPI Canada", "Index", Frequency.Monthly),
                ["Rates"] = new("IRSTCI01CAM156N", "Interest Rate Canada", "%", Frequency.Monthly),
                ["Unemployment"] = new("LRHUTTTTCAM156S", "Unemployment Rate Canada", "%", Frequency.Monthly)
            },
            ["CHF"] = new()
            {
                ["GDP"] = new("CHEGDPRQPSMEI", "GDP Switzerland", "Index", Frequency.Quarterly),
                ["CPI"] = new("CHECPIALLMINMEI", "CPI Switzerland", "Index", Frequency.Monthly),
                ["Rates"] = new("IRSTCI01CHM156N", "Interest Rate Switzerland", "%", Frequency.Monthly),
                ["Unemployment"] = new("LRHUTTTTCHM156S", "Unemployment Rate Switzerland", "%", Frequency.Monthly)
            }
        };

        private readonly IAsyncPolicy<HttpResponseMessage> _retryPolicy;
        private readonly IAsyncPolicy<HttpResponseMessage> _circuitBreakerPolicy;

        public FredService(
            IHttpClientFactory httpClientFactory,
            IMemoryCache cache,
            ILogger<FredService> logger,
            IConfiguration configuration,
            IOptions<FredServiceOptions>? options = null,
            AuditTrailEngine? auditTrail = null)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _options = options?.Value ?? new FredServiceOptions();
            _auditTrail = auditTrail;

            _retryPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .Or<TimeoutException>()
                .WaitAndRetryAsync(
                    _options.MaxRetries,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (outcome, timeSpan, retryCount, context) =>
                    {
                        _logger.LogWarning(
                            "Retry {RetryCount} after {Delay}s due to {Error}",
                            retryCount,
                            timeSpan.TotalSeconds,
                            outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString());
                    });

            _circuitBreakerPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .CircuitBreakerAsync(
                    _options.CircuitBreakerThreshold,
                    TimeSpan.FromSeconds(_options.CircuitBreakerDurationSeconds),
                    onBreak: (outcome, duration) =>
                    {
                        _logger.LogError(
                            "Circuit breaker opened for {Duration}s due to {Error}",
                            duration.TotalSeconds,
                            outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString());
                    },
                    onReset: () =>
                    {
                        _logger.LogInformation("Circuit breaker reset");
                    });
        }

        public Task<MacroData> GetMacroDataAsync(CancellationToken cancellationToken = default)
        {
            return GetMacroDataAsync(null, cancellationToken);
        }

        public async Task<MacroData> GetMacroDataAsync(
            string[]? currencies = null,
            CancellationToken cancellationToken = default)
        {
            var normalizedCurrencies = NormalizeCurrencies(currencies);
            var cacheKey = BuildCacheKey(normalizedCurrencies);

            if (_cache.TryGetValue<MacroData>(cacheKey, out var cached))
            {
                if (!IsStale(cached))
                {
                    _logger.LogDebug("Cache hit for macro data");
                    return cached;
                }

                _logger.LogDebug("Cache hit but data is stale, refreshing in background");
                _ = RefreshMacroDataAsync(normalizedCurrencies, CancellationToken.None);

                return cached;
            }

            return await FetchAndCacheMacroDataAsync(normalizedCurrencies, cancellationToken);
        }

        public Task<MacroData> RefreshMacroDataAsync(CancellationToken cancellationToken = default)
        {
            return RefreshMacroDataAsync(null, cancellationToken);
        }

        public async Task<MacroData> RefreshMacroDataAsync(
            string[]? currencies = null,
            CancellationToken cancellationToken = default)
        {
            var normalizedCurrencies = NormalizeCurrencies(currencies);
            var cacheKey = BuildCacheKey(normalizedCurrencies);

            _cache.Remove(cacheKey);

            return await FetchAndCacheMacroDataAsync(normalizedCurrencies, cancellationToken);
        }

        public async Task<object> GetCurrencyMacroDataAsync(
            string currency,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(currency))
                throw new ArgumentException("Currency is required.", nameof(currency));

            var normalizedCurrency = currency.Trim().ToUpperInvariant();
            var macroData = await GetMacroDataAsync(new[] { normalizedCurrency }, cancellationToken);

            if (macroData.Data.TryGetValue(normalizedCurrency, out var currencyData))
                return currencyData;

            throw new KeyNotFoundException($"Currency '{normalizedCurrency}' was not found.");
        }

        public Dictionary<string, Dictionary<string, string>> GetFredSeriesMappings()
        {
            return FredSeries.ToDictionary(
                currency => currency.Key,
                currency => currency.Value.ToDictionary(
                    indicator => indicator.Key,
                    indicator => indicator.Value.SeriesId));
        }

        public async Task<FredStatusResponse> CheckStatusAsync(
            CancellationToken cancellationToken = default)
        {
            var resolvedApiKey = ResolveApiKey();

            var response = new FredStatusResponse
            {
                IsConfigured = !string.IsNullOrWhiteSpace(resolvedApiKey),
                ApiKeyProvided = !string.IsNullOrWhiteSpace(resolvedApiKey) ? "Yes (masked)" : "No",
                CheckedAtUtc = DateTime.UtcNow,
                IsConnected = false,
                Message = "Not connected to FRED API",
                SeriesAvailability = new Dictionary<string, bool>(),
                RateLimitRemaining = null
            };

            if (!response.IsConfigured)
            {
                response.Message =
                    "FRED API key not configured. Add 'FRED_API_KEY' to appsettings.json or environment variables.";

                return response;
            }

            try
            {
                var client = CreateHttpClient();

                var testUrl =
                    $"series/observations?series_id=FEDFUNDS&api_key={resolvedApiKey}&file_type=json&limit=1";

                using var testResponse = await client.GetAsync(testUrl, cancellationToken);

                response.IsConnected = testResponse.IsSuccessStatusCode;
                response.Message = testResponse.IsSuccessStatusCode
                    ? "Successfully connected to FRED API"
                    : $"FRED API error: {testResponse.StatusCode}";

                var sampleSeries = new[] { "FEDFUNDS", "UNRATE", "CPIAUCSL" };

                foreach (var seriesId in sampleSeries)
                {
                    try
                    {
                        var url = $"series?series_id={seriesId}&api_key={resolvedApiKey}&file_type=json";
                        using var seriesResponse = await client.GetAsync(url, cancellationToken);

                        response.SeriesAvailability[seriesId] = seriesResponse.IsSuccessStatusCode;
                    }
                    catch
                    {
                        response.SeriesAvailability[seriesId] = false;
                    }
                }

                if (testResponse.Headers.TryGetValues("X-RateLimit-Remaining", out var rateLimit))
                {
                    response.RateLimitRemaining =
                        int.TryParse(rateLimit.FirstOrDefault(), out var limit) ? limit : null;
                }
            }
            catch (Exception ex)
            {
                response.IsConnected = false;
                response.Message = $"Connection error: {ex.Message}";
            }

            return response;
        }

        private async Task<MacroData> FetchAndCacheMacroDataAsync(
            string[]? currencies = null,
            CancellationToken cancellationToken = default)
        {
            var normalizedCurrencies = NormalizeCurrencies(currencies);
            var cacheKey = BuildCacheKey(normalizedCurrencies);
            var semaphore = _locks.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));

            await semaphore.WaitAsync(cancellationToken);

            try
            {
                if (_cache.TryGetValue<MacroData>(cacheKey, out var cached) && !IsStale(cached))
                    return cached;

                var apiKey = ResolveApiKey();

                if (string.IsNullOrWhiteSpace(apiKey))
                    throw new InvalidOperationException("FRED API key is not configured.");

                var targetCurrencies = normalizedCurrencies ?? FredSeries.Keys.ToArray();

                var result = new ConcurrentDictionary<string, CurrencyMacroData>();
                var failedCurrencies = new ConcurrentBag<string>();
                var warnings = new ConcurrentBag<string>();

                using var httpClient = CreateHttpClient();
                using var throttle = new SemaphoreSlim(_options.MaxConcurrentRequests);

                var tasks = targetCurrencies.Select(async currency =>
                {
                    await throttle.WaitAsync(cancellationToken);

                    try
                    {
                        if (!FredSeries.TryGetValue(currency, out var seriesMap))
                        {
                            failedCurrencies.Add(currency);
                            warnings.Add($"No FRED series mapping for {currency}");
                            _logger.LogWarning("No FRED series mapping for {Currency}", currency);
                            return;
                        }

                        var fetchResult = await FetchCurrencyDataAsync(
                            httpClient,
                            currency,
                            seriesMap,
                            apiKey,
                            cancellationToken);

                        if (fetchResult.Success)
                        {
                            result[currency] = fetchResult.Data;

                            if (!string.IsNullOrWhiteSpace(fetchResult.Warning))
                                warnings.Add(fetchResult.Warning);

                            return;
                        }

                        failedCurrencies.Add(currency);
                    }
                    catch (Exception ex)
                    {
                        failedCurrencies.Add(currency);

                        _logger.LogError(
                            ex,
                            "Failed to fetch FRED macro data for {Currency}",
                            currency);
                    }
                    finally
                    {
                        throttle.Release();
                    }
                });

                await Task.WhenAll(tasks);

                var data = result.ToDictionary(x => x.Key, x => x.Value);

                if (data.Count == 0)
                    throw new InvalidOperationException("FRED returned no usable macro data.");

                if (!failedCurrencies.IsEmpty)
                {
                    warnings.Add(
                        $"FRED failed for currencies: {string.Join(", ", failedCurrencies.Distinct().OrderBy(x => x))}");
                }

                var macroData = CreateMacroData(
                    data,
                    warnings.IsEmpty
                        ? null
                        : string.Join(" | ", warnings.Distinct()));

                _cache.Set(cacheKey, macroData, CreateCacheOptions());

                if (_auditTrail != null)
                {
                    await _auditTrail.RecordEventAsync(
                        Guid.NewGuid(),
                        "FredDataFetched",
                        new Dictionary<string, object>
                        {
                            ["Currencies"] = targetCurrencies.Length,
                            ["Successful"] = data.Values.Count(x => x.IsLiveData),
                            ["Failed"] = failedCurrencies.Distinct().Count()
                        });
                }

                return macroData;
            }
            finally
            {
                semaphore.Release();
            }
        }

        private async Task<(bool Success, CurrencyMacroData Data, string? Warning)> FetchCurrencyDataAsync(
            HttpClient client,
            string currency,
            Dictionary<string, FredSeriesInfo> seriesMap,
            string apiKey,
            CancellationToken cancellationToken)
        {
            var currencyData = new CurrencyMacroData();
            var fetchTasks = new Dictionary<string, Task<decimal?>>();

            foreach (var indicator in seriesMap)
            {
                fetchTasks[indicator.Key] = indicator.Key switch
                {
                    "CPI" => FetchYoYChangeAsync(client, indicator.Value.SeriesId, apiKey, cancellationToken),
                    _ => FetchLatestValueAsync(client, indicator.Value.SeriesId, apiKey, cancellationToken)
                };
            }

            await Task.WhenAll(fetchTasks.Values);

            var gdp = GetResult(fetchTasks, "GDP");
            var inflation = GetResult(fetchTasks, "CPI");
            var rates = GetResult(fetchTasks, "Rates");
            var unemployment = GetResult(fetchTasks, "Unemployment");

            var hasAnyLiveData =
                gdp.HasValue ||
                inflation.HasValue ||
                rates.HasValue ||
                unemployment.HasValue;

            if (!hasAnyLiveData)
            {
                _logger.LogWarning("No live FRED data returned for {Currency}", currency);
                return (false, currencyData, $"No live FRED data returned for {currency}");
            }

            var missingFields = new List<string>();

            if (!gdp.HasValue)
                missingFields.Add("GDP");

            if (!inflation.HasValue)
                missingFields.Add("CPI");

            if (!rates.HasValue)
                missingFields.Add("Rates");

            if (!unemployment.HasValue)
                missingFields.Add("Unemployment");

            currencyData.GDP = gdp ?? 0m;
            currencyData.Inflation = inflation ?? 0m;
            currencyData.Rates = rates ?? 0m;
            currencyData.Unemployment = unemployment ?? 0m;
            currencyData.LastUpdated = DateTime.UtcNow;
            currencyData.IsLiveData = true;

            var warning = missingFields.Count > 0
                ? $"{currency} missing FRED fields: {string.Join(", ", missingFields)}"
                : null;

            if (warning != null)
                _logger.LogWarning("{Warning}", warning);

            return (true, currencyData, warning);
        }

        private async Task<decimal?> FetchLatestValueAsync(
            HttpClient client,
            string seriesId,
            string apiKey,
            CancellationToken cancellationToken)
        {
            try
            {
                var url =
                    $"series/observations?series_id={seriesId}&api_key={apiKey}&file_type=json&limit=1&sort_order=desc";

                using var response = await _retryPolicy
                    .WrapAsync(_circuitBreakerPolicy)
                    .ExecuteAsync(() => client.GetAsync(url, cancellationToken));

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "FRED API returned {StatusCode} for series {SeriesId}",
                        response.StatusCode,
                        seriesId);

                    return null;
                }

                var json = await response.Content.ReadAsStringAsync(cancellationToken);

                using var doc = JsonDocument.Parse(json);

                if (!doc.RootElement.TryGetProperty("observations", out var observations))
                    return null;

                if (observations.GetArrayLength() == 0)
                    return null;

                var valueElement = observations[0].GetProperty("value");

                if (valueElement.ValueKind == JsonValueKind.String &&
                    decimal.TryParse(valueElement.GetString(), out var value))
                {
                    return value;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to fetch latest value for series {SeriesId}", seriesId);
                return null;
            }
        }

        private async Task<decimal?> FetchYoYChangeAsync(
            HttpClient client,
            string seriesId,
            string apiKey,
            CancellationToken cancellationToken)
        {
            try
            {
                var url =
                    $"series/observations?series_id={seriesId}&api_key={apiKey}&file_type=json&limit=13&sort_order=desc";

                using var response = await _retryPolicy
                    .WrapAsync(_circuitBreakerPolicy)
                    .ExecuteAsync(() => client.GetAsync(url, cancellationToken));

                if (!response.IsSuccessStatusCode)
                    return null;

                var json = await response.Content.ReadAsStringAsync(cancellationToken);

                using var doc = JsonDocument.Parse(json);

                if (!doc.RootElement.TryGetProperty("observations", out var observations))
                    return null;

                var values = new List<decimal>();

                foreach (var obs in observations.EnumerateArray())
                {
                    if (!obs.TryGetProperty("value", out var valueElement))
                        continue;

                    if (valueElement.ValueKind == JsonValueKind.String &&
                        decimal.TryParse(valueElement.GetString(), out var value))
                    {
                        values.Add(value);
                    }
                }

                if (values.Count >= 13)
                {
                    var current = values[0];
                    var yearAgo = values[12];

                    if (yearAgo != 0)
                        return Math.Round(((current - yearAgo) / yearAgo) * 100, 2);
                }

                if (values.Count >= 2)
                {
                    var current = values[0];
                    var previous = values[1];

                    if (previous != 0)
                    {
                        var monthlyChange = (current - previous) / previous;
                        return Math.Round(monthlyChange * 12 * 100, 2);
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to calculate YoY change for series {SeriesId}", seriesId);
                return null;
            }
        }

        private HttpClient CreateHttpClient()
        {
            var client = _httpClientFactory.CreateClient("FRED");

            client.BaseAddress = new Uri("https://api.stlouisfed.org/fred/");
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            client.Timeout = TimeSpan.FromSeconds(_options.RequestTimeoutSeconds);

            return client;
        }

        private string ResolveApiKey()
        {
            return _configuration["FRED_API_KEY"]
                   ?? _configuration["FredApi:Key"]
                   ?? _options.ApiKey
                   ?? Environment.GetEnvironmentVariable("FRED_API_KEY");
        }

        private bool IsStale(MacroData data)
        {
            if (!data.IsLive)
                return false;

            return DateTime.UtcNow - data.LastUpdated >
                   TimeSpan.FromSeconds(_options.StaleDataThresholdSeconds);
        }

        private MemoryCacheEntryOptions CreateCacheOptions()
        {
            return new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromSeconds(_options.CacheExpirationSeconds))
                .SetSlidingExpiration(TimeSpan.FromSeconds(_options.CacheSlidingExpirationSeconds));
        }

        private static string[]? NormalizeCurrencies(string[]? currencies)
        {
            if (currencies == null || currencies.Length == 0)
                return null;

            return currencies
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim().ToUpperInvariant())
                .Distinct()
                .OrderBy(x => x)
                .ToArray();
        }

        private static string BuildCacheKey(string[]? currencies)
        {
            return currencies?.Any() == true
                ? $"{CACHE_KEY_PREFIX}_{string.Join("_", currencies)}"
                : CACHE_KEY_PREFIX;
        }

        private static MacroData CreateMacroData(
            Dictionary<string, CurrencyMacroData> data,
            string? warning = null)
        {
            return new MacroData
            {
                Data = data,
                IsLive = true,
                LastUpdated = DateTime.UtcNow,
                Warning = warning,
                DataSource = "FRED API"
            };
        }

        private static decimal? GetResult(
            Dictionary<string, Task<decimal?>> tasks,
            string key)
        {
            return tasks.TryGetValue(key, out var task)
                ? task.Result
                : null;
        }
    }
}