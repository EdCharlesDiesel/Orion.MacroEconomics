namespace Orion.MacroEconomics.DTO;

public sealed class IndicatorResult
{
    public decimal Open  { get; set; }
    public decimal High  { get; set; }
    public decimal Low   { get; set; }
    public decimal Close { get; set; }
    public decimal EMA20 { get; set; }
    public decimal EMA50 { get; set; }
    public decimal MACD  { get; set; }
    public decimal MACDSignal { get; set; }
    public decimal RSI    { get; set; }
    public decimal StochK { get; set; }
    public decimal StochD { get; set; }
    public decimal ATR     { get; set; }
    public decimal BBUpper { get; set; }
    public decimal BBLower { get; set; }
    public decimal ADX { get; set; }
    public string  Indicator { get; set; } = string.Empty;
    public decimal ZScore    { get; set; }
    public decimal Surprise  { get; set; }
    public decimal YoY       { get; set; }
}