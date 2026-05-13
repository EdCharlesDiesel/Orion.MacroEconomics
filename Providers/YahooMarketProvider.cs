using Orion.MacroEconomics.Entities;
using Orion.MacroEconomics.Providers.Interfaces;

namespace Orion.MacroEconomics.Providers
{
    public sealed class YahooMarketProvider(ILogger<YahooMarketProvider> logger) : IYahooMarketProvider
    {
        public string Name => "Yahoo";

        public async Task<object> GetAsync(
            string symbol,
            DateTime fromUtc,
            DateTime toUtc,
            CancellationToken cancellationToken = default)
        {
            var request = new MarketDataRequest
            {
                Pair = symbol,
                From = fromUtc,
                To = toUtc,
                Interval = "1d",
                Provider = Name
            };

            return await GetHistoricalCandlesAsync(request, cancellationToken);
        }

        public async Task<IReadOnlyList<OhlcvBar>> GetHistoricalCandlesAsync(
            MarketDataRequest request,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);

            if (string.IsNullOrWhiteSpace(request.Pair))
                throw new ArgumentException("Pair is required.", nameof(request));

            var symbol = ToYahooSymbol(request.Pair);
            var period = ToPeriod(request.Interval);

            var from = request.From == default
                ? DateTime.UtcNow.AddMonths(-3)
                : request.From;

            var to = request.To == default
                ? DateTime.UtcNow
                : request.To;

            logger.LogInformation(
                "Fetching Yahoo candles for {Pair} using symbol {Symbol}",
                request.Pair,
                symbol);

            var candles = await Yahoo.GetHistoricalAsync(symbol, from, to, period);

            return candles
                .OrderBy(x => x.DateTime)
                .Select(x => new OhlcvBar
                {
                    Pair = request.Pair.Trim().ToUpperInvariant(),
                    TimestampUtc = x.DateTime,
                    Open = Convert.ToDecimal(x.Open),
                    High = Convert.ToDecimal(x.High),
                    Low = Convert.ToDecimal(x.Low),
                    Close = Convert.ToDecimal(x.Close),
                    Volume = Convert.ToDecimal(x.Volume)
                })
                .ToList();
        }

        public async Task<MarketQuote?> GetLatestQuoteAsync(
            string pair,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(pair))
                throw new ArgumentException("Pair is required.", nameof(pair));

            var symbol = ToYahooSymbol(pair);

            logger.LogInformation(
                "Fetching Yahoo latest quote for {Pair} using symbol {Symbol}",
                pair,
                symbol);

            var securities = await Yahoo.Symbols(symbol)
                .Fields(
                    Field.RegularMarketPrice,
                    Field.RegularMarketOpen,
                    Field.RegularMarketDayHigh,
                    Field.RegularMarketDayLow,
                    Field.RegularMarketPreviousClose,
                    Field.RegularMarketVolume)
                .QueryAsync();

            if (!securities.TryGetValue(symbol, out var security))
                return null;

            var price = Convert.ToDecimal(security[Field.RegularMarketPrice]);

            return new MarketQuote
            {
                Pair = pair.Trim().ToUpperInvariant(),
                Bid = price,
                Ask = price,
                Last = price,
                TimestampUtc = DateTime.UtcNow,
                Source = "Yahoo Finance"
            };
        }

        public async Task<MarketTick?> GetLatestTickAsync(
            string pair,
            CancellationToken cancellationToken = default)
        {
            var quote = await GetLatestQuoteAsync(pair, cancellationToken);

            if (quote == null)
                return null;

            return new MarketTick
            {
                Pair = quote.Pair,
                Bid = quote.Bid,
                Ask = quote.Ask,
                Time = quote.TimestampUtc,
                Source = quote.Source
            };
        }

        public async Task<MarketDataHealth> CheckHealthAsync(
            string pair,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(pair))
                throw new ArgumentException("Pair is required.", nameof(pair));

            try
            {
                var quote = await GetLatestQuoteAsync(pair, cancellationToken);

                return new MarketDataHealth
                {
                    Provider = Name,
                    Pair = pair.Trim().ToUpperInvariant(),
                    IsHealthy = quote != null,
                    Message = quote != null
                        ? "Yahoo market data provider is healthy."
                        : "Yahoo market data provider returned no quote.",
                    CheckedAtUtc = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Yahoo provider health check failed for {Pair}", pair);

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

        public async Task<List<MarketDataResponse>> FetchDataAsync(
            string pair,
            string interval,
            string period,
            CancellationToken cancellationToken = default)
        {
            var request = new MarketDataRequest
            {
                Pair = pair,
                Interval = interval,
                From = ResolveFrom(period),
                To = DateTime.UtcNow,
                Provider = Name
            };

            var candles = await GetHistoricalCandlesAsync(request, cancellationToken);

            return candles.Select(x => new MarketDataResponse
            {
                Pair = x.Pair,
                TimestampUtc = x.TimestampUtc,
                OhlcvBar = new List<OhlcvBar>()
                {
                    new()
                    {
                        Open = x.Open,
                        High = x.High,
                        Low = x.Low,
                        Close = x.Close,
                        Source = Name,
                        Volume = x.Volume,
                    }
                }
            }).ToList();
        }

        public async Task<Dictionary<string, MarketDataResponse>> FetchAllTimeframesAsync(string pair, CancellationToken cancellationToken = default)
        {
            var timeframes = new[] { "1d", "1wk", "1mo" };
            var result = new Dictionary<string, MarketDataResponse>();

            foreach (var timeframe in timeframes)
            {
                var data = await FetchDataAsync(
                    pair,
                    timeframe,
                    "3mo",
                    cancellationToken);

                var latest = data
                    .OrderByDescending(x => x.TimestampUtc)
                    .FirstOrDefault();

                if (latest != null)
                    result[timeframe] = latest;
            }

            return result;
        }

        public async Task<List<KpiData>> GetKpisAsync(
            CancellationToken cancellationToken = default)
        {
            var pairs = new[] { "EUR/USD", "GBP/USD", "USD/JPY", "USD/CHF" };
            var result = new List<KpiData>();

            foreach (var pair in pairs)
            {
                var quote = await GetLatestQuoteAsync(pair, cancellationToken);

                if (quote == null)
                    continue;

                result.Add(new KpiData
                {
                    Name = pair,
                    Value = quote.Last,
                    Source = Name,
                    TimestampUtc = quote.TimestampUtc
                });
            }

            return result;
        }

        private static DateTime ResolveFrom(string? period)
        {
            return period?.Trim().ToLowerInvariant() switch
            {
                "1d" => DateTime.UtcNow.AddDays(-1),
                "5d" => DateTime.UtcNow.AddDays(-5),
                "1mo" => DateTime.UtcNow.AddMonths(-1),
                "3mo" => DateTime.UtcNow.AddMonths(-3),
                "6mo" => DateTime.UtcNow.AddMonths(-6),
                "1y" => DateTime.UtcNow.AddYears(-1),
                "2y" => DateTime.UtcNow.AddYears(-2),
                "5y" => DateTime.UtcNow.AddYears(-5),
                _ => DateTime.UtcNow.AddMonths(-3)
            };
        }

        private static string ToYahooSymbol(string pair)
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

            return $"{clean}=X";
        }

        private static Period ToPeriod(string? interval)
        {
            return interval?.Trim().ToLowerInvariant() switch
            {
                "1d" => Period.Daily,
                "1wk" => Period.Weekly,
                "1w" => Period.Weekly,
                "1mo" => Period.Monthly,
                "1m" => Period.Monthly,
                _ => Period.Daily
            };
        }
    }
}