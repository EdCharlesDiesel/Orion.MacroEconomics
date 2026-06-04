namespace Orion.MacroEconomics.Models;


public class TrendSignalResult
{
    public string Pair { get; set; } = string.Empty;
    public bool Error { get; set; }
    public string Signal { get; set; } = string.Empty;
    public int Score { get; set; }
    public int MaxScore { get; set; }
    public string Direction { get; set; } = string.Empty;
    public Dictionary<string, bool> Conditions { get; set; } = new();
    public double Close { get; set; }
    public double Ema50 { get; set; }
    public double Ema200 { get; set; }
    public double Rsi { get; set; }
    public double Macd { get; set; }
    public double Adx { get; set; }
}

public class AtrData
{
    public double Atr14 { get; set; }
    public double Atr20 { get; set; }
    public double Atr14Pips { get; set; }
    public double Atr20Pips { get; set; }
    public double SlPips { get; set; }
    public double Tp1Pips { get; set; }
    public double Tp2Pips { get; set; }
    public bool AtrOk { get; set; }
}

public class MtfAlignment
{
    public string? Weekly { get; set; }
    public string? Daily { get; set; }
    public string? FourHour { get; set; }
    public int Aligned { get; set; }
    public int Total { get; set; }
    public string Target { get; set; } = string.Empty;
}

public class SessionStatus
{
    public string Window { get; set; } = string.Empty;
    public bool Prime { get; set; }
    public string Color { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Time { get; set; } = string.Empty;
}

public class DailyLossLimit
{
    public int LossesToday { get; set; }
    public int Limit { get; set; }
    public bool Blocked { get; set; }
}

public class CorrelationWarning
{
    public string Group { get; set; } = string.Empty;
    public string Conflicting { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class TradingStats
{
    public int Total { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public int Be { get; set; }
    public double WinRate { get; set; }
    public double AvgWinR { get; set; }
    public double AvgLossR { get; set; }
    public double Expectancy { get; set; }
    public double ProfitFactor { get; set; }
}

public class MarketData
{
    public DateTime Date { get; set; }
    public double Open { get; set; }
    public double High { get; set; }
    public double Low { get; set; }
    public double Close { get; set; }
    public long Volume { get; set; }
    public double Ema50 { get; set; }
    public double Ema200 { get; set; }
    public double Rsi { get; set; }
    public double Macd { get; set; }
    public double MacdSig { get; set; }
    public double MacdHist { get; set; }
    public double Adx { get; set; }
    public double PlusDi { get; set; }
    public double MinusDi { get; set; }
}