// using JasperFx;
// using Marten;
// using Orion.MacroEconomics.Entities;
// using Orion.MacroEconomics.Jobs;
// using Orion.MacroEconomics.Models;
// using Orion.MacroEconomics.Repository;
// using Orion.MacroEconomics.Repository.Interfaces;
// using Orion.MacroEconomics.Services;
//
// namespace Orion.MacroEconomics.Extensions;
//
// public static class MassiveExtensions
// {
//     /// <summary>
//     /// Registers the Massive Forex typed HTTP client, the market-data
//     /// repository, the trade-plan factory, and the background polling
//     /// service. Call this from Program.cs after AddMarten(...).
//     /// </summary>
//     public static IServiceCollection AddMassiveForex(this IServiceCollection services, IConfiguration configuration)
//     {
//         var baseUrl = configuration["Massive:BaseUrl"] ?? "https://api.massive.com/";
//         var apiKey  = configuration["Massive:ApiKey"]
//             ?? throw new InvalidOperationException("Missing Massive:ApiKey in configuration.");
//
//         services.AddHttpClient<MassiveClient>(client =>
//         {
//             client.BaseAddress = new Uri(baseUrl);
//             client.Timeout     = TimeSpan.FromSeconds(30);
//             client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
//         });
//
//         // Repository — scoped so each unit-of-work (web request or background
//         // scope) gets its own instance. The underlying IDocumentStore is the
//         // shared singleton, so this is cheap.
//         services.AddScoped<IMarketDataRepository, MarketDataRepository>();
//
//         // TradePlanFactory depends only on IOptions<AppConfiguration> (singleton)
//         // and ILogger (singleton), so it is safe to register as singleton and
//         // inject directly into the hosted service without creating a scope.
//         services.AddSingleton<TradePlanFactory>();
//
//         services.AddHostedService<MassiveBackgroundService>();
//
//         return services;
//     }
//
//     /// <summary>
//     /// Stand-alone Marten registration for projects that don't already
//     /// configure Marten themselves. If you already call AddMarten(...) in
//     /// Program.cs, DO NOT call this — use <see cref="ConfigureMassiveForexSchemas"/>
//     /// inside your existing AddMarten lambda instead.
//     /// </summary>
//     public static IServiceCollection AddMassiveForexMarten(this IServiceCollection services, IConfiguration configuration)
//     {
//         services.AddMarten(options =>
//         {
//             options.Connection(
//                 configuration.GetConnectionString("OrionMacroDbConnection")
//                 ?? throw new InvalidOperationException(
//                     "Missing ConnectionStrings:OrionMacroDbConnection"));
//
//             options.AutoCreateSchemaObjects = AutoCreate.All;
//
//             ConfigureMassiveForexSchemas(options);
//
//             options.UseSystemTextJsonForSerialization(configure: settings =>
//             {
//                 settings.WriteIndented = true;
//             });
//         })
//         .UseLightweightSessions()
//         .ApplyAllDatabaseChangesOnStartup();
//
//         return services;
//     }
//
//     /// <summary>
//     /// Configures every document schema the Massive background service
//     /// writes to. Call this from inside your AddMarten(options => { ... })
//     /// lambda alongside any other schemas your project owns.
//     /// </summary>
//     public static void ConfigureMassiveForexSchemas(StoreOptions options)
//     {
//         // Reference documents (upserted on startup)
//         options.Schema.For<ForexReferenceDocument>()
//             .Identity(x => x.Id);
//
//         options.Schema.For<ForexExchangeDocument>()
//             .Identity(x => x.Id);
//
//         // Market status documents
//         options.Schema.For<MarketStatusDocument>()
//             .Identity(x => x.Id);
//
//         options.Schema.For<MarketHolidayDocument>()
//             .Identity(x => x.Id);
//
//         options.Schema.For<TopMoversDocument>()
//             .Identity(x => x.Id);
//
//         // Quote and snapshot documents with indexes
//         options.Schema.For<ForexQuoteDocument>()
//             .Identity(x => x.Id)
//             .Index(x => x.Pair);
//
//         options.Schema.For<ForexSnapshotDocument>()
//             .Identity(x => x.Id)
//             .Index(x => x.Ticker)
//             .Index(x => x.UpdatedUtc);
//
//         // Technical indicators
//         options.Schema.For<TechnicalIndicatorDocument>()
//             .Identity(x => x.Id)
//             .Index(x => x.Ticker)
//             .Index(x => x.UpdatedUtc);
//
//         // Currency conversions
//         options.Schema.For<CurrencyConversionDocument>()
//             .Identity(x => x.Id);
//
//         // Candle/bar data
//         options.Schema.For<CandleDocument>()
//             .Identity(x => x.Id)
//             .Index(x => x.Pair)
//             .Index(x => x.Timeframe)
//             .Index(x => x.LastUpdated);
//
//         // Previous day bars
//         options.Schema.For<PreviousDayBarDocument>()
//             .Identity(x => x.Id)
//             .Index(x => x.Ticker);
//
//         // Daily market summaries
//         options.Schema.For<DailyMarketSummaryDocument>()
//             .Identity(x => x.Id)
//             .Index(x => x.Date);
//
//         // Trade plans — each row is a unique plan (insert-only)
//         options.Schema.For<TradePlan>()
//             .Identity(x => x.Id)
//             .Index(x => x.Pair)
//             .Index(x => x.OpenedAt)
//             .Index(x => x.Status);
//     }
// }