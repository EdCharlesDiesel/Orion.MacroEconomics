namespace Orion.MacroEconomics.DTO;

public class IndicatorResult
{
    public decimal BBLower;
    public decimal BBUpper;
    public decimal MACDSignal;
    public const decimal RSI = 0;
    public const decimal ADX = 0;
    public const decimal ATR = 0;
    public const decimal EMA20 = 0;
    public const decimal MACD = 0;
    public const decimal EMA50 = 0;
    public decimal Close { get; set; }
    public decimal Low { get; set; }
    public decimal High { get; set; }
    public decimal StochD { get; set; }
    public decimal StochK { get; set; }
}