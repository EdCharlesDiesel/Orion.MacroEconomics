
using Orion.MacroEconomics.Engine.Interfaces;
using Orion.MacroEconomics.Entities;
using Orion.MacroEconomics.Repository.Interfaces;
using Orion.MacroEconomics.Services;

namespace Orion.MacroEconomics.Engine;

public class NewsEngine(
    IForexEventRepository eventRepository,
    IForexNewsRepository newsRepository,
    ForexEventScraperService scraperService,
    ILogger<NewsEngine> logger)
    : INewsEngine
{
    public async Task<IEnumerable<ForexEvent>> FetchAndProcessEventsAsync()
    {
        logger.LogInformation("Starting event fetch and process cycle");

        var newEvents = new List<ForexEvent>();

        // Fetch from ForexFactory
        var scraperEvents = await scraperService.FetchEventsFromForexFactoryAsync();

        // Fetch from alternative API
        var apiEvents = await scraperService.FetchEventsFromAlternativeApiAsync();

        var allEvents = scraperEvents.Concat(apiEvents);

        foreach (var evt in allEvents)
        {
            if (!await eventRepository.ExistsAsync(evt.EventId))
            {
                await eventRepository.AddAsync(evt);
                newEvents.Add(evt);
                logger.LogInformation("Added new event: {Title} for {Currency}", evt.Title, evt.Currency);
            }
            else
            {
                // Update existing event if needed
                var existing = await eventRepository.GetByEventIdAsync(evt.EventId);
                if (existing != null && (evt.Actual != existing.Actual || evt.Forecast != existing.Forecast))
                {
                    existing.Actual = evt.Actual;
                    existing.Forecast = evt.Forecast;
                    existing.Previous = evt.Previous;
                    await eventRepository.UpdateAsync(existing);
                    logger.LogInformation("Updated event: {Title}", evt.Title);
                }
            }
        }

        return newEvents;
    }
    public async Task<IEnumerable<ForexNews>> FetchAndProcessNewsAsync()
    {
        logger.LogInformation("Starting news fetch and process cycle");

        var newNews = new List<ForexNews>();

        // This would integrate with a news API
        // For now, returning mock data
        var mockNews = GenerateMockNews();

        foreach (var news in mockNews)
        {
            if (!await newsRepository.ExistsAsync(news.NewsId))
            {
                await newsRepository.AddAsync(news);
                newNews.Add(news);
                logger.LogInformation("Added new news: {Title}", news.Title);
            }
        }

        return newNews;
    }

    public async Task<Dictionary<string, List<ForexEvent>>> GetEventsByCurrencyAsync(DateTime? from = null)
    {
        var startDate = from ?? DateTime.UtcNow.AddDays(-30);
        var allEvents = await eventRepository.GetAllAsync(1, 1000);

        return allEvents
            .Where(e => e.EventDate >= startDate)
            .GroupBy(e => e.Currency)
            .ToDictionary(g => g.Key, g => g.ToList());
    }
    public async Task<List<ForexEvent>> GetHighImpactEventsAsync(int days = 3)
    {
        var events = await eventRepository.GetUpcomingEventsAsync(days);
        return events.Where(e => e.Impact == "High").ToList();
    }
    public async Task<Dictionary<string, decimal>> CalculateCurrencyStrengthAsync()
    {
        var strength = new Dictionary<string, decimal>();
        var currencies = new[] { "USD", "EUR", "GBP", "JPY", "CAD", "AUD", "CHF", "NZD" , "ZAR" };

        foreach (var currency in currencies)
        {
            var events = await eventRepository.GetByCurrencyAsync(currency, DateTime.UtcNow.AddDays(-7));
            var highImpactEvents = events.Count(e => e.Impact == "High");
            var positiveEvents = events.Count(e => e.Actual > e.Forecast);

            // Simple strength calculation based on recent events
            var score = (positiveEvents * 10) - (highImpactEvents * 5);
            strength[currency] = Math.Max(-100, Math.Min(100, score));
        }

        return strength;
    }
    public async Task<ForexEvent?> GetNextMajorEventAsync(string? currency = null)
    {
        var events = await eventRepository.GetUpcomingEventsAsync(7);
        var highImpactEvents = events.Where(e => e.Impact == "High");

        if (!string.IsNullOrEmpty(currency))
            highImpactEvents = highImpactEvents.Where(e => e.Currency == currency);

        return highImpactEvents.OrderBy(e => e.EventDate).FirstOrDefault();
    }
    private List<ForexNews> GenerateMockNews()
    {
        return new List<ForexNews>
        {
            new ForexNews
            {
                NewsId = Guid.NewGuid().ToString(),
                Title = "Fed signals potential rate cuts in coming months",
                Content = "Federal Reserve officials indicated...",
                Source = "Reuters",
                PublishedAt = DateTime.UtcNow.AddHours(-2),
                AffectedCurrencies = new List<string> { "USD" },
                SentimentScore = 25
            },
            new ForexNews
            {
                NewsId = Guid.NewGuid().ToString(),
                Title = "ECB remains cautious on inflation outlook",
                Content = "European Central Bank maintains...",
                Source = "Bloomberg",
                PublishedAt = DateTime.UtcNow.AddHours(-5),
                AffectedCurrencies = new List<string> { "EUR" },
                SentimentScore = -10
            }
        };
    }
}