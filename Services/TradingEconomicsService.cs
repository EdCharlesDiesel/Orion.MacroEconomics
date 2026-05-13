using Orion.MacroEconomics.Entities;
using Orion.MacroEconomics.Interfaces;

namespace Orion.MacroEconomics.Services;

/// <summary>
/// Implements Trading Economics REST API calls for news and intraday market data.
///
/// Register in DI:
///   builder.Services.AddHttpClient&lt;ITradingEconomicsService, TradingEconomicsService&gt;(client =>
///   {
///       client.BaseAddress = new Uri("https://api.tradingeconomics.com/");
///   });
///
/// Required appsettings.json entry:
///   "TradingEconomics": { "ApiKey": "YOUR_API_KEY" }
/// </summary>
public sealed class TradingEconomicsService : ITradingEconomicsService
{
    private const string DateFormat = "yyyy-MM-dd HH:mm";

    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly ILogger<TradingEconomicsService> _logger;

    public TradingEconomicsService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<TradingEconomicsService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _apiKey = configuration["TradingEconomics:ApiKey"]
                  ?? throw new InvalidOperationException(
                      "TradingEconomics:ApiKey is not configured.");
    }

    

    /// <inheritdoc/>
    public async Task<IReadOnlyList<TradingEconomicsNewsItem>> GetLatestNewsAsync(
        CancellationToken cancellationToken = default)
    {
        var url = BuildUrl("news");

        _logger.LogInformation("Fetching latest Trading Economics news");

        return await GetAsync<List<TradingEconomicsNewsItem>>(url, cancellationToken)
               ?? [];
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<TradingEconomicsNewsItem>> GetNewsByCountryAsync(
        string[] countries,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(countries);

        if (countries.Length == 0)
            throw new ArgumentException("At least one country is required.", nameof(countries));

        var joined = string.Join(",", countries.Select(Uri.EscapeDataString));
        var url = BuildUrl($"news/country/{joined}");

        _logger.LogInformation(
            "Fetching Trading Economics news for countries: {Countries}",
            string.Join(", ", countries));

        return await GetAsync<List<TradingEconomicsNewsItem>>(url, cancellationToken)
               ?? [];
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<TradingEconomicsNewsItem>> GetNewsByIndicatorAsync(
        string[] indicators,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(indicators);

        if (indicators.Length == 0)
            throw new ArgumentException("At least one indicator is required.", nameof(indicators));

        var joined = string.Join(",", indicators.Select(Uri.EscapeDataString));
        var url = BuildUrl($"news/indicator/{joined}");

        _logger.LogInformation(
            "Fetching Trading Economics news for indicators: {Indicators}",
            string.Join(", ", indicators));

        return await GetAsync<List<TradingEconomicsNewsItem>>(url, cancellationToken)
               ?? [];
    }



    /// <inheritdoc/>
    public async Task<IReadOnlyList<TradingEconomicsIntradayBar>> GetIntradaySymbolAsync(
        string symbol,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            throw new ArgumentException("Symbol is required.", nameof(symbol));

        var url = BuildUrl($"markets/intraday/{Uri.EscapeDataString(symbol)}");

        _logger.LogInformation("Fetching intraday data for symbol: {Symbol}", symbol);

        return await GetAsync<List<TradingEconomicsIntradayBar>>(url, cancellationToken)
               ?? [];
    }


    public async Task<IReadOnlyList<TradingEconomicsIntradayBar>> GetIntradayDateHourAsync(
        string symbol,
        DateTime startDate,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            throw new ArgumentException("Symbol is required.", nameof(symbol));

        var d1 = Uri.EscapeDataString(startDate.ToString(DateFormat));
        var url = BuildUrl($"markets/intraday/{Uri.EscapeDataString(symbol)}", $"d1={d1}");

        _logger.LogInformation(
            "Fetching intraday data for {Symbol} from {StartDate}",
            symbol, startDate);

        return await GetAsync<List<TradingEconomicsIntradayBar>>(url, cancellationToken)
               ?? [];
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<TradingEconomicsIntradayBar>> GetIntradaySymbolDatesAsync(
        string symbol,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(symbol))
            throw new ArgumentException("Symbol is required.", nameof(symbol));

        if (startDate >= endDate)
            throw new ArgumentException("startDate must be before endDate.");

        var d1 = Uri.EscapeDataString(startDate.ToString(DateFormat));
        var d2 = Uri.EscapeDataString(endDate.ToString(DateFormat));
        var url = BuildUrl($"markets/intraday/{Uri.EscapeDataString(symbol)}", $"d1={d1}&d2={d2}");

        _logger.LogInformation(
            "Fetching intraday data for {Symbol} from {StartDate} to {EndDate}",
            symbol, startDate, endDate);

        return await GetAsync<List<TradingEconomicsIntradayBar>>(url, cancellationToken)
               ?? [];
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds a fully-qualified Trading Economics API URL, appending the API key.
    /// </summary>
    private string BuildUrl(string path, string? extraParams = null)
    {
        var url = $"{path}?c={_apiKey}";

        if (!string.IsNullOrWhiteSpace(extraParams))
            url += $"&{extraParams}";

        return url;
    }

    /// <summary>
    /// Sends a GET request and deserialises the JSON response.
    /// Returns null on a 404; throws on all other non-success codes.
    /// </summary>
    private async Task<T?> GetAsync<T>(string url, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(url, cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Trading Economics returned 404 for: {Url}", url);
            return default;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken);
    }

    public async Task<object?> GetBetweenCountries(string country1, string country2, int pageNumber)
    {
        throw new NotImplementedException();
    }
}