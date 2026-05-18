using Marten;
using Microsoft.AspNetCore.Mvc;
using Orion.MacroEconomics.Entities;
using Orion.MacroEconomics.Extensions;
using static Orion.MacroEconomics.Enum.TradeDirection;
using TradePlan = Orion.MacroEconomics.Extensions.TradePlan;
using TradePlanStatus = Orion.MacroEconomics.Extensions.TradePlanStatus;

namespace Orion.MacroEconomics.Controllers;

[ApiController]
[Route("api/trade-plans")]
public sealed class TradePlanController(
    IDocumentSession  session,
    IQuerySession     query,
    TradePlanFactory  factory,
    ILogger<TradePlanController> logger) : ControllerBase
{
    /// <summary>
    /// Generate trade plans from stored candle data for all pairs.
    /// </summary>
    [HttpPost("generate")]
    public async Task<IActionResult> GenerateAsync(CancellationToken ct)
    {
        var pairs = new[] { "EUR/USD", "GBP/USD", "USD/JPY", "USD/ZAR", "XAU/USD" , "AUD/USD", "USD/CAD","AUD/JPY", };
        var created = new List<TradePlan>();

        foreach (var pair in pairs)
        {
            // Load stored weekly candles from Marten
            var doc = await query.LoadAsync<CandleDocument>(
                CandleDocument.BuildId(pair, "Weekly"), ct);

            if (doc is null || doc.Candles.Count < 20)
            {
                logger.LogWarning("Insufficient candle data for {Pair}", pair);
                continue;
            }

            var plan = factory.CreateFromCandles(pair, doc.Candles);
            if (plan is null) continue;

            // Check no pending plan already exists for this pair
            var existing = await query.Query<TradePlan>()
                .AnyAsync(p => p.Pair   == pair
                            && p.Status == TradePlanStatus.Pending, ct);

            if (existing)
            {
                logger.LogInformation(
                    "Skipping {Pair} — pending plan already exists", pair);
                continue;
            }

            session.Store(plan);
            created.Add(plan);
        }

        await session.SaveChangesAsync(ct);

        return Ok(new
        {
            Generated = created.Count,
            Plans     = created.Select(p => new
            {
                p.Id, p.Pair, p.Direction,
                p.EntryPrice, p.StopLoss, p.TakeProfit1, p.TakeProfit2,
                p.RiskReward, p.Reasoning
            })
        });
    }

    /// <summary>Get all pending trade plans.</summary>
    [HttpGet("pending")]
    public async Task<IActionResult> GetPendingAsync(CancellationToken ct)
    {
        var plans = await query.Query<TradePlan>()
            .Where(p => p.Status == TradePlanStatus.Pending)
            .OrderByDescending(p => p.OpenedAt)
            .ToListAsync(ct);

        return Ok(plans);
    }

    /// <summary>Get a specific trade plan.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var plan = await query.LoadAsync<TradePlan>(id, ct);
        return plan is null ? NotFound() : Ok(plan);
    }

    /// <summary>Close a trade plan.</summary>
    [HttpPatch("{id:guid}/close")]
    public async Task<IActionResult> CloseAsync(Guid id, [FromBody] ClosePlanRequest request, CancellationToken ct)
    {
        var plan = await session.LoadAsync<TradePlan>(id, ct);
        if (plan is null) return NotFound();
        plan.Status     = TradePlanStatus.Closed;
        plan.ClosedAt   = DateTime.UtcNow;
        plan.ClosePrice = request.ClosePrice;
        plan.PnL        = plan.Direction == Long.ToString() ? request.ClosePrice - plan.EntryPrice : plan.EntryPrice - request.ClosePrice;

        session.Store(plan);
        await session.SaveChangesAsync(ct);

        return Ok(plan);
    }
}

public sealed record ClosePlanRequest(decimal ClosePrice);