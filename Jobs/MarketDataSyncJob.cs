using Orion.MacroEconomics.Services;
using Quartz;

namespace Orion.MacroEconomics.Jobs;

/// <summary>
/// Quartz job that triggers a full market data sync on a configurable schedule.
/// Default: every 6 hours.  Override via appsettings: "Quartz:MarketDataCron".
/// </summary>
[DisallowConcurrentExecution]
public sealed class MarketDataSyncJob(MarketDataSyncService sync, ILogger<MarketDataSyncJob> log) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        log.LogInformation("MarketDataSyncJob triggered at {Time}", DateTimeOffset.UtcNow);
        try
        {
            await sync.SyncAllAsync(context.CancellationToken);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "MarketDataSyncJob failed");
            throw new JobExecutionException(ex, refireImmediately: false);
        }
    }
}