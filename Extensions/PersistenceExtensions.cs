using JasperFx;
using Marten;
using Orion.MacroEconomics.Entities;
using Orion.MacroEconomics.Jobs;
using Orion.MacroEconomics.Repository;
using Orion.MacroEconomics.Repository.Interfaces;
using Orion.MacroEconomics.Services;
using Quartz;

namespace Orion.MacroEconomics.Extensions;

public static class PersistenceExtensions
{
    /// <summary>
    /// Registers Marten (document store), the market-data repository,
    /// the sync service, and a Quartz job that keeps the data fresh.
    ///
    /// Required config keys:
    ///   ConnectionStrings:Postgres  — standard Npgsql connection string
    ///   Quartz:MarketDataCron       — optional cron override (default: every 6 h)
    /// </summary>
    public static IServiceCollection AddMarketDataPersistence(this IServiceCollection services, IConfiguration          configuration)
    {
        var connString = configuration.GetConnectionString("Postgres")
                         ?? throw new InvalidOperationException(
                             "Missing ConnectionStrings:Postgres in configuration.");


        services.AddMarten(opts =>
        {
            opts.Connection(connString);

            // Auto-create / migrate schema in dev; use MigrationRunnerStyle.None
            // in production and run `dotnet run -- db-apply` from your pipeline.
            opts.AutoCreateSchemaObjects = AutoCreate.CreateOrUpdate;

            // Store all market-data documents in a dedicated schema.
            opts.DatabaseSchemaName = "market";

            // ── Document configuration ─────────────────────────────────
            opts.Schema.For<CandleDocument>()
                .Identity(x => x.Id)
                .Index(x => x.Pair)
                .Index(x => x.Timeframe);

            opts.Schema.For<MacroSnapshotDocument>()
                .Identity(x => x.Id);   // currency code

            opts.Schema.For<EconomySeriesDocument>()
                .Identity(x => x.Id);   // series id
        })
        .UseLightweightSessions()        // no identity map overhead for read-heavy workloads
        .ApplyAllDatabaseChangesOnStartup(); // ensures schema exists before first request


        services.AddScoped<IMarketDataRepository, MarketDataRepository>();
        services.AddScoped<MarketDataSyncService>();


        var cron = configuration["Quartz:MarketDataCron"] ?? "0 0 0/6 * * ?"; // every 6 h

        services.AddQuartz(q =>
        {
            var jobKey = new JobKey("MarketDataSync");

            q.AddJob<MarketDataSyncJob>(opts => opts.WithIdentity(jobKey));

            q.AddTrigger(opts => opts
                .ForJob(jobKey)
                .WithIdentity("MarketDataSync-trigger")
                .WithCronSchedule(cron,
                    x => x.InTimeZone(TimeZoneInfo.Utc)));
        });

        services.AddQuartzHostedService(q => q.WaitForJobsToComplete = true);

        return services;
    }
}