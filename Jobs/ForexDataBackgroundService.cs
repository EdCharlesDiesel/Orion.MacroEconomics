using Orion.MacroEconomics.Engine.Interfaces;
using Orion.MacroEconomics.Repository.Interfaces;
namespace Orion.MacroEconomics.Jobs;

public class ForexDataBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<ForexDataBackgroundService> logger)
    : BackgroundService
{
    private readonly TimeSpan _eventFetchInterval = TimeSpan.FromHours(6); // Fetch events every 6 hours
    private readonly TimeSpan _newsFetchInterval = TimeSpan.FromHours(1);  // Fetch news every hour

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Forex Data Background Service is starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var forexEngine = scope.ServiceProvider.GetRequiredService<INewsEngine>();
                var eventRepository = scope.ServiceProvider.GetRequiredService<IForexEventRepository>();
                var newsRepository = scope.ServiceProvider.GetRequiredService<IForexNewsRepository>();

                // Fetch events
                logger.LogInformation("Fetching latest forex events");
                var newEvents = await forexEngine.FetchAndProcessEventsAsync();
                logger.LogInformation("Fetched {Count} new events", newEvents.Count());

                // Fetch news
                logger.LogInformation("Fetching latest forex news");
                var newNews = await forexEngine.FetchAndProcessNewsAsync();
                logger.LogInformation("Fetched {Count} new news articles", newNews.Count());

                // Clean up old data (keep last 90 days)
                await CleanupOldDataAsync(eventRepository, newsRepository);

                // Log statistics
                await LogStatisticsAsync(eventRepository, newsRepository);

                // Wait before next fetch cycle
                await Task.Delay(_eventFetchInterval, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred in background service");
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); // Wait before retry
            }
        }
    }

    private Task CleanupOldDataAsync(IForexEventRepository eventRepository, IForexNewsRepository newsRepository)
    {
        // Marten can handle this with document policies, but here's manual cleanup logic
        logger.LogDebug("Cleaning up old data (placeholder for Marten document cleanup)");
        // You can implement Marten's document lifecycle policies instead
        return Task.CompletedTask;
    }

    private async Task LogStatisticsAsync(IForexEventRepository eventRepository, IForexNewsRepository newsRepository)
    {
        var eventCount = await eventRepository.GetCountAsync();
        logger.LogInformation("Total events in database: {Count}", eventCount);
    }
}