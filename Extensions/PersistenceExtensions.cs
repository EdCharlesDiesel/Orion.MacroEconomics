using Orion.MacroEconomics.Jobs;
using Orion.MacroEconomics.Services;
using Quartz;

namespace Orion.MacroEconomics.Extensions;

public static class PersistenceExtensions
{
    /// <summary>
    /// Registers MarketDataSyncService and the Quartz job that streams
    /// data from Massive every minute.
    /// Override schedule via appsettings: "Quartz:MarketDataCron"
    /// </summary>
    public static IServiceCollection AddMarketDataPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<MarketDataSyncService>();

        // Every minute: "0 * * * * ?" — override in appsettings if needed
        var cron = configuration["Quartz:MarketDataCron"] ?? "0 * * * * ?";

        services.AddQuartz(q =>
        {
            var jobKey = new JobKey("MarketIngestion");

            q.AddJob<MarketIngestionJob>(opts => opts.WithIdentity(jobKey));

            q.AddTrigger(opts => opts
                .ForJob(jobKey)
                .WithIdentity("MarketIngestion-trigger")
                .WithCronSchedule(cron, x => x.InTimeZone(TimeZoneInfo.Utc)));
        });

        services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

        return services;
    }
}