using Orion.MacroEconomics.Models;

namespace Orion.MacroEconomics.Services;

public class RiskManagementService
{
    private readonly ILogger<RiskManagementService> _logger;
    private readonly IConfiguration _configuration;

    public RiskManagementService(ILogger<RiskManagementService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public (bool IsValid, string Reason) ValidateTrade(
        StrategySignal signal,
        decimal accountEquity,
        int tradesToday,
        decimal dailyPnL,
        List<TradeExecution> openTrades)
    {
        // Rule 1: Max daily loss (3%)
        decimal maxDailyLossPercent = _configuration.GetValue<decimal>("Strategy:MaxDailyLossPercent", 3m);
        if (dailyPnL <= -accountEquity * (maxDailyLossPercent / 100))
        {
            return (false, $"Daily loss limit of {maxDailyLossPercent}% reached");
        }

        // Rule 2: Max daily trades (3)
        int maxDailyTrades = _configuration.GetValue<int>("Strategy:MaxDailyTrades", 3);
        if (tradesToday >= maxDailyTrades)
        {
            return (false, $"Daily trade limit of {maxDailyTrades} reached");
        }

        // Rule 3: Max open positions (concurrent trades)
        int maxOpenPositions = _configuration.GetValue<int>("Strategy:MaxOpenPositions", 2);
        if (openTrades.Count >= maxOpenPositions)
        {
            return (false, $"Maximum open positions ({maxOpenPositions}) reached");
        }

        // Rule 4: Minimum confidence (70%)
        int minConfidence = _configuration.GetValue<int>("Strategy:MinConfidence", 70);
        if (signal.Confidence < minConfidence)
        {
            return (false, $"Confidence {signal.Confidence}% below minimum {minConfidence}%");
        }

        // Rule 5: Risk/Reward ratio (minimum 1:2)
        decimal riskPips = CalculatePips(Math.Abs(signal.Entry - signal.StopLoss), signal.Symbol);
        decimal rewardPips = CalculatePips(Math.Abs(signal.TakeProfits[1] - signal.Entry), signal.Symbol);

        if (rewardPips / riskPips < 2)
        {
            return (false, $"Risk/Reward ratio {rewardPips/riskPips:F1} below minimum 2.0");
        }

        // Rule 6: Maximum risk per trade (2%)
        decimal maxRiskPercent = _configuration.GetValue<decimal>("Strategy:MaxRiskPerTrade", 2m);
        decimal riskAmount = accountEquity * (maxRiskPercent / 100);
        decimal positionSize = riskAmount / (riskPips * 10); // Assuming $10 per pip per lot

        if (positionSize > accountEquity / 20) // Max 5% of account per trade
        {
            return (false, "Position size exceeds maximum allowed");
        }

        return (true, "Trade validated");
    }

    public decimal CalculatePositionSize(decimal accountEquity, decimal stopLossPips, decimal riskPercent = 2m)
    {
        decimal riskAmount = accountEquity * (riskPercent / 100);
        decimal pipValue = 10; // Standard lot = $10 per pip
        return riskAmount / (stopLossPips * pipValue);
    }

    private decimal CalculatePips(decimal priceDifference, string symbol)
    {
        // Most forex pairs have 4 decimal places = 1 pip
        // JPY pairs have 2 decimal places
        if (symbol.Contains("JPY"))
            return priceDifference * 100;
        return priceDifference * 10000;
    }

    public (decimal TP1, decimal TP2, decimal TP3) GetScaledTakeProfits(
        TradeExecution trade, decimal currentPrice)
    {
        if (trade.Direction == "LONG")
        {
            return (
                TP1: trade.TakeProfits["TP1"],
                TP2: trade.TakeProfits["TP2"],
                TP3: trade.TakeProfits["TP3"]
            );
        }
        else
        {
            return (
                TP1: trade.TakeProfits["TP1"],
                TP2: trade.TakeProfits["TP2"],
                TP3: trade.TakeProfits["TP3"]
            );
        }
    }

    public (bool ShouldClose, string Reason) CheckTrailingStop(
        TradeExecution trade, decimal currentPrice, decimal atr)
    {
        if (trade.Direction == "LONG")
        {
            decimal trailStop = currentPrice - (atr * 2);
            if (trailStop > trade.StopLoss)
            {
                return (true, "Trailing stop triggered");
            }
        }
        else
        {
            decimal trailStop = currentPrice + (atr * 2);
            if (trailStop < trade.StopLoss)
            {
                return (true, "Trailing stop triggered");
            }
        }

        return (false, "");
    }
}