using Microsoft.EntityFrameworkCore;
using Npgsql;
using Orion.MacroEconomics.Data;
using Orion.MacroEconomics.Models;

namespace Orion.MacroEconomics.Services;

public class DatabaseService(OrionMacroEconomicsDbContext context, IConfiguration configuration)
{
    private readonly string _connectionString = configuration.GetConnectionString("TradingDb")!;

    public async Task InitializeDatabaseAsync()
    {
        await context.Database.EnsureCreatedAsync();

        // Run migrations for outcome-tracking columns
        using var conn = new NpgsqlConnection(_connectionString);
        await conn.OpenAsync();

        var columns = new[] { "entry_price", "outcome", "close_price", "pips_gained", "r_multiple", "is_open" };
        foreach (var col in columns)
        {
            try
            {
                using var cmd = new NpgsqlCommand(
                    $"ALTER TABLE trade_setups ADD COLUMN IF NOT EXISTS {col} " +
                    (col == "entry_price" || col == "close_price" || col == "pips_gained" || col == "r_multiple"
                        ? "FLOAT" : col == "outcome" ? "VARCHAR(10)" : "BOOLEAN DEFAULT TRUE"),
                    conn);
                await cmd.ExecuteNonQueryAsync();
            }
            catch { /* Column may already exist */ }
        }
    }

    public async Task<int> SaveTradeAsync(TradeSetup trade)
    {
        context.TradeSetups.Add(trade);
        await context.SaveChangesAsync();
        return trade.Id;
    }

    public async Task<List<TradeSetup>> LoadTradesAsync(int limit = 50)
    {
        return (List<TradeSetup>)await context.TradeSetups
            .OrderByDescending(t => t.LoggedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task DeleteTradeAsync(int id)
    {
        var trade = await context.TradeSetups.FindAsync(id);
        if (trade != null)
        {
            context.TradeSetups.Remove(trade);
            await context.SaveChangesAsync();
        }
    }

    public async Task CloseTradeAsync(int id, double entryPrice, double closePrice,
        double pipsGained, double rMultiple, string outcome)
    {
        var trade = await context.TradeSetups.FindAsync(id);
        if (trade != null)
        {
            trade.EntryPrice = entryPrice;
            trade.ClosePrice = closePrice;
            trade.PipsGained = pipsGained;
            trade.RMultiple = rMultiple;
            trade.Outcome = outcome;
            trade.IsOpen = false;
            await context.SaveChangesAsync();
        }
    }

    public async Task<DailyLossLimit> GetDailyLossLimitAsync(int maxLosses = 2)
    {
        var today = DateTime.UtcNow.Date;
        var count = await context.TradeSetups
            .CountAsync(t => t.Outcome == "LOSS" &&
                           !t.IsOpen &&
                           t.LoggedAt.Date == today);

        return new DailyLossLimit
        {
            LossesToday = count,
            Limit = maxLosses,
            Blocked = count >= maxLosses
        };
    }

    public async Task<List<TradeSetup>> GetOpenTradesAsync()
    {
        return (List<TradeSetup>)await context.TradeSetups
            .Where(t => t.IsOpen)
            .OrderByDescending(t => t.LoggedAt)
            .Take(20)
            .ToListAsync();
    }

    public async Task<TradingStats?> GetStatsAsync(int n = 20)
    {
        var trades = await context.TradeSetups
            .Where(t => !t.IsOpen && t.Outcome != null)
            .OrderByDescending(t => t.LoggedAt)
            .Take(n)
            .ToListAsync();

        if (!trades.Any()) return null;

        var wins = trades.Where(t => t.Outcome == "WIN").ToList();
        var losses = trades.Where(t => t.Outcome == "LOSS").ToList();
        var beTrades = trades.Where(t => t.Outcome == "BE").ToList();

        int total = trades.Count;
        double winRate = total > 0 ? (double)wins.Count / total * 100 : 0;

        var winRs = wins.Where(w => w.RMultiple.HasValue).Select(w => w.RMultiple!.Value).ToList();
        var lossRs = losses.Where(l => l.RMultiple.HasValue).Select(l => Math.Abs(l.RMultiple!.Value)).ToList();

        double avgWinR = winRs.Any() ? winRs.Average() : 0;
        double avgLossR = lossRs.Any() ? lossRs.Average() : 1.0;
        double lossRate = total > 0 ? (double)losses.Count / total : 0;
        double expectancy = (winRate / 100 * avgWinR) - (lossRate * avgLossR);
        double profitFactor = (losses.Any() && avgLossR > 0)
            ? (wins.Count * avgWinR) / (losses.Count * avgLossR)
            : 0;

        return new TradingStats
        {
            Total = total,
            Wins = wins.Count,
            Losses = losses.Count,
            Be = beTrades.Count,
            WinRate = winRate,
            AvgWinR = avgWinR,
            AvgLossR = avgLossR,
            Expectancy = expectancy,
            ProfitFactor = profitFactor
        };
    }
}