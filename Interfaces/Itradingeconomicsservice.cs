using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Interfaces;

/// <summary>
/// Provides access to the Trading Economics REST API for news and intraday market data.
/// </summary>
public interface ITradingEconomicsService
{
    /// <summary>
    /// Returns the latest news articles across all countries and categories.
    /// </summary>
    Task<IReadOnlyList<TradingEconomicsNewsItem>> GetLatestNewsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the latest news filtered by one or more countries (e.g. "united states", "germany").
    /// </summary>
    Task<IReadOnlyList<TradingEconomicsNewsItem>> GetNewsByCountryAsync(
        string[] countries,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the latest news filtered by one or more indicators (e.g. "gdp", "inflation rate").
    /// </summary>
    Task<IReadOnlyList<TradingEconomicsNewsItem>> GetNewsByIndicatorAsync(
        string[] indicators,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns intraday OHLC bars for a given market symbol (e.g. "aapl:us").
    /// Supports up to 5 comma-separated symbols.
    /// </summary>
    Task<IReadOnlyList<TradingEconomicsIntradayBar>> GetIntradaySymbolAsync(
        string symbol,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns intraday OHLC bars for a symbol starting from a specific date/hour.
    /// Only returns data from the last 30 days.
    /// </summary>
    Task<IReadOnlyList<TradingEconomicsIntradayBar>> GetIntradayDateHourAsync(
        string symbol,
        DateTime startDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns intraday OHLC bars for a symbol between two dates.
    /// Only returns data from the last 30 days.
    /// </summary>
    Task<IReadOnlyList<TradingEconomicsIntradayBar>> GetIntradaySymbolDatesAsync(
        string symbol,
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);
}