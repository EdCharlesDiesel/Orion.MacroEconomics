namespace Orion.MacroEconomics.DTO;

public sealed class IndicatorResult
{
    public DateTime Date { get; set; }

    public decimal Open  { get; set; }
    public decimal High  { get; set; }
    public decimal Low   { get; set; }
    public decimal Close { get; set; }

    // Indicator values are nullable because they require a warm-up window before they become defined.
    public decimal? EMA20 { get; set; }
    public decimal? EMA50 { get; set; }

    public decimal? MACD          { get; set; }
    public decimal? MACDSignal    { get; set; }
    public decimal? MACDHistogram { get; set; }

    public decimal? RSI    { get; set; }
    public decimal? StochK { get; set; }
    public decimal? StochD { get; set; }
    public decimal? ATR    { get; set; }

    public decimal? BBUpper  { get; set; }
    public decimal? BBMiddle { get; set; }
    public decimal? BBLower  { get; set; }

    public decimal? ADX    { get; set; }
    public decimal? ADXPos { get; set; }
    public decimal? ADXNeg { get; set; }

    public decimal? Resistance20 { get; set; }
    public decimal? Support20    { get; set; }

    public string  Indicator   { get; set; } = string.Empty;
    public decimal ZScore      { get; set; }
    public decimal Surprise    { get; set; }
    public decimal YoY         { get; set; }

    // QuantConnect-style classification output set by TechnicalAnalyzer.GenerateProSignals.
    public string Signal      { get; set; } = "HOLD";
    public int    SignalScore { get; set; }
}
