using Orion.MacroEconomics.Commands;
using Orion.MacroEconomics.Services;
using MediatR;
using Marten;
using Orion.MacroEconomics.Entities;
using Quartz;

namespace Orion.MacroEconomics.Jobs;

[DisallowConcurrentExecution]
public sealed class MarketIngestionJob(IMediator mediator, MarketDataSyncService sync, IDocumentSession session, ILogger<MarketIngestionJob> logger) : IJob
{
    private static readonly string[] Countries =
    [
        "United States", "Euro Area",
        "Japan", "United Kingdom", "South Africa"
    ];

    public async Task Execute(IJobExecutionContext context)
    {
        var ct  = context.CancellationToken;
        var run = new IngestionRunDocument
        {
            Id          = Guid.NewGuid(),
            TriggeredAt = DateTimeOffset.UtcNow
        };

        logger.LogInformation("MarketIngestionJob started. RunId={RunId}", run.Id);

        foreach (var country in Countries)
        {
            var result = new CountryIngestionResult { Country = country };
            try
            {
                result.RecordsIngested = await mediator.Send(
                    new IngestMacroDataCommand(country), ct);

                result.Success = true;
                logger.LogInformation("[{Country}] Ingested {Count} macro records",
                    country, result.RecordsIngested);
            }
            catch (Exception ex)
            {
                result.Success      = false;
                result.ErrorMessage = ex.Message;
                logger.LogError(ex, "[{Country}] Macro ingestion failed", country);
            }

            run.MacroResults.Add(result);
        }

        // ── Market data sync ───────────────────────────────────────────────────
        try
        {
            await sync.SyncAllAsync(ct);
            run.MarketSyncSuccess = true;
            logger.LogInformation("Market data sync completed. RunId={RunId}", run.Id);
        }
        catch (Exception ex)
        {
            run.MarketSyncSuccess      = false;
            run.MarketSyncErrorMessage = ex.Message;
            logger.LogError(ex, "Market data sync failed. RunId={RunId}", run.Id);
        }

        // ── Persist run record ─────────────────────────────────────────────────
        run.CompletedAt     = DateTimeOffset.UtcNow;
        run.DurationSeconds = (run.CompletedAt - run.TriggeredAt).TotalSeconds;
        run.TotalRecords    = run.MacroResults.Sum(x => x.RecordsIngested);
        run.FullySuccessful = run.MarketSyncSuccess
                              && run.MacroResults.All(x => x.Success);

        session.Store(run);
        await session.SaveChangesAsync(ct);

        logger.LogInformation(
            "MarketIngestionJob completed. RunId={RunId} Duration={Duration:F1}s " +
            "TotalRecords={Total} Success={Success}",
            run.Id, run.DurationSeconds, run.TotalRecords, run.FullySuccessful);

        if (!run.FullySuccessful)
            throw new JobExecutionException(
                new Exception("One or more ingestion tasks failed — see run document for details."),
                refireImmediately: false);
    }
}