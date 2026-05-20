// using Orion.MacroEconomics.Configurations;
// using Orion.MacroEconomics.Jobs;
// using Orion.MacroEconomics.Providers.Interfaces;
// using Orion.MacroEconomics.Repository.Interfaces;
//
// namespace Orion.MacroEconomics.Services;
//
// /// <summary>
// /// Orchestrates pulling data from <see cref="IMassiveDataProvider"/> and
// /// persisting it to Postgres via <see cref="IMarketDataRepository"/>.
// ///
// /// This service is registered as a scoped dependency and is called by:
// ///  • <see cref="MarketDataSyncJob"/> (Quartz, scheduled)
// ///  • API endpoints that need a fresh fetch on demand
// /// </summary>
// public sealed class MarketDataSyncService(IMassiveDataProvider provider, IMarketDataRepository    repository, ILogger<MarketDataSyncService> log)
// {
//     // All series IDs supported by GetSeriesAsync
//     private static readonly string[] SeriesIds =
//     [
//         "FEDFUNDS", "CPIAUCSL", "UNRATE",
//         "A191RL1Q225SBEA", "DGS10", "DGS2",
//         "DTWEXBGS", "VIXCLS",
//     ];
//
//     /// <summary>Sync all candles for every pair/timeframe in AppConfig.</summary>
//     public async Task SyncCandlesAsync(CancellationToken ct = default)
//     {
//         foreach (var pair in AppConfig.Assets.Keys)
//         {
//             foreach (var timeframe in AppConfig.Timeframes.Keys)
//             {
//                 try
//                 {
//                     var candles = await provider.GetCandlesAsync(pair, timeframe, ct);
//                     if (candles.Count > 0)
//                     {
//                              await repository.UpsertCandlesAsync(pair, timeframe, candles, ct);
//                     }
//
//                 }
//                 catch (Exception ex)
//                 {
//                     log.LogError(ex, "Candle sync failed for {Pair}/{Tf}", pair, timeframe);
//                 }
//             }
//         }
//     }
//
//     /// <summary>Sync macro snapshots for all currencies.</summary>
//     public async Task SyncMacroAsync(CancellationToken ct = default)
//     {
//         try
//         {
//             var (snapshots, isLive) = await provider.GetAllMacroAsync(ct);
//             // await repository.UpsertMacroSnapshotsAsync(snapshots, isLive, ct);
//         }
//         catch (Exception ex)
//         {
//             log.LogError(ex, "Macro snapshot sync failed");
//         }
//     }
//
//     /// <summary>Sync all economy time-series.</summary>
//     public async Task SyncSeriesAsync(CancellationToken ct = default)
//     {
//         foreach (var seriesId in SeriesIds)
//         {
//             try
//             {
//                 var points = await provider.GetSeriesAsync(seriesId, limit: 60, ct);
//                 if (points.Count > 0)
//                     await repository.UpsertSeriesAsync(seriesId, points, ct);
//             }
//             catch (Exception ex)
//             {
//                 log.LogError(ex, "Series sync failed for {SeriesId}", seriesId);
//             }
//         }
//     }
//
//     /// <summary>Full sync: candles + macro + all series.</summary>
//     public async Task SyncAllAsync(CancellationToken ct = default)
//     {
//         log.LogInformation("Full market data sync started");
//         await SyncMacroAsync(ct);
//         await SyncSeriesAsync(ct);
//         await SyncCandlesAsync(ct);
//         log.LogInformation("Full market data sync complete");
//     }
// }