using Marten.Schema;

namespace Orion.MacroEconomics.Models;

public sealed class ForexSnapshotDocument
{
    [Identity]
    public string Id { get; set; } = string.Empty; // ticker_timestamp
    public string Ticker { get; set; } = string.Empty;
    public ForexSnapshot Snapshot { get; set; } = null!;
    public DateTime CapturedAt { get; set; }
    public decimal  TodaysChange    { get; set; }
    public decimal  TodaysChangePct { get; set; }
    public decimal  DayOpen         { get; set; }
    public decimal  DayHigh         { get; set; }
    public decimal  DayLow          { get; set; }
    public decimal  DayClose        { get; set; }
    public decimal  DayVolume       { get; set; }
    public decimal  PrevClose       { get; set; }
    public decimal  Bid             { get; set; }
    public decimal  Ask             { get; set; }
    public DateTime UpdatedUtc      { get; set; }
}

public sealed class ForexTickerDocument
{
    [Identity]
    public string Ticker { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string BaseCurrency { get; set; } = string.Empty;
    public string QuoteCurrency { get; set; } = string.Empty;
    public bool Active { get; set; }
    public DateTime LastUpdated { get; set; }
}

public sealed class ForexQuoteDocument
{
    [Identity]
    public string Id { get; set; } = string.Empty; // ticker_sequenceNumber
    public string Ticker { get; set; } = string.Empty;
    public ForexQuote Quote { get; set; } = null!;
    public DateTime CapturedAt { get; set; }
    public string   Pair       { get; set; } = "";
    public decimal  Bid        { get; set; }
    public decimal  Ask        { get; set; }
    public decimal  Mid        { get; set; }
    public int      Exchange   { get; set; }
    public DateTime QuoteTime  { get; set; }
    public DateTime UpdatedUtc { get; set; }
}

public sealed class ConversionDocument
{
    [Identity]
    public string Id { get; set; } = string.Empty; // from_to_timestamp
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public ConversionResult Conversion { get; set; } = null!;
    public DateTime CapturedAt { get; set; }
}

public sealed class IndicatorDocument
{
    [Identity]
    public string Id { get; set; } = string.Empty; // ticker_indicatorType_timestamp
    public string Ticker { get; set; } = string.Empty;
    public string IndicatorType { get; set; } = string.Empty;
    public IndicatorValue Value { get; set; } = null!;
    public DateTime CapturedAt { get; set; }
}

public sealed class MarketStatusDocument
{
    [Identity]
    public string Id { get; set; } = string.Empty; // date
    public MarketStatus Status { get; set; } = null!;
    public DateTime CapturedAt { get; set; }
    public bool     FxOpen     { get; set; }
    public string   Market     { get; set; } = "";
    public string   ServerTime { get; set; } = "";
    public DateTime UpdatedUtc { get; set; }
}

public sealed class MarketHolidayDocument
{
    [Identity]
    public string Id { get; set; } = string.Empty; // exchange_date
    public string Exchange { get; set; } = string.Empty;
    public MarketHoliday Holiday { get; set; } = null!;
    public DateTime CapturedAt { get; set; }
    public List<HolidayRecord> Holidays  { get; set; } = [];
    public DateTime            UpdatedUtc { get; set; }
    public DateTime HolidayDate { get; set; }
}

public sealed class ForexReferenceDocument
{
    public string              Id         { get; set; } = "";
    public List<ForexTickerRecord> Tickers { get; set; } = [];
    public DateTime            UpdatedUtc { get; set; }
}

public sealed class ForexTickerRecord
{
    public string Ticker       { get; set; } = "";
    public string Name         { get; set; } = "";
    public string BaseCurrency { get; set; } = "";
    public bool   Active       { get; set; }
}

public sealed class ForexExchangeDocument
{
    public string              Id        { get; set; } = "";
    public List<ExchangeRecord> Exchanges { get; set; } = [];
    public DateTime            UpdatedUtc { get; set; }
}

public sealed class ExchangeRecord
{
    public int    Id      { get; set; }
    public string Name    { get; set; } = "";
    public string Acronym { get; set; } = "";
    public string Mic     { get; set; } = "";
    public string Type    { get; set; } = "";
}

public sealed class HolidayRecord
{
    public string Exchange { get; set; } = "";
    public string Name     { get; set; } = "";
    public DateTime Date     { get; set; }
    public string Status   { get; set; } = "";
}

public sealed class TopMoversDocument
{
    public string          Id         { get; set; } = "";
    public List<MoverRecord> Gainers  { get; set; } = [];
    public List<MoverRecord> Losers   { get; set; } = [];
    public DateTime        UpdatedUtc { get; set; }
}

public sealed class MoverRecord
{
    public string  Ticker    { get; set; } = "";
    public decimal ChangePct { get; set; }
    public decimal Change    { get; set; }
    public decimal DayClose  { get; set; }
}

public sealed class CurrencyConversionDocument
{
    public string   Id         { get; set; } = "";
    public string   From       { get; set; } = "";
    public string   To         { get; set; } = "";
    public decimal  Rate       { get; set; }
    public decimal  Bid        { get; set; }
    public decimal  Ask        { get; set; }
    public DateTime UpdatedUtc { get; set; }
}

public sealed class TechnicalIndicatorDocument
{
    public string   Id         { get; set; } = "";
    public string   Ticker     { get; set; } = "";
    public decimal  Sma20      { get; set; }
    public decimal  Sma50      { get; set; }
    public decimal  Ema20      { get; set; }
    public decimal  Ema50      { get; set; }
    public decimal  Rsi14      { get; set; }
    public decimal  MacdValue  { get; set; }
    public decimal  MacdSignal { get; set; }
    public decimal  MacdHist   { get; set; }
    public DateTime UpdatedUtc { get; set; }
}
public sealed class PreviousDayBarDocument
{
    public string   Id         { get; set; } = "";
    public string   Ticker     { get; set; } = "";
    public decimal  Open       { get; set; }
    public decimal  High       { get; set; }
    public decimal  Low        { get; set; }
    public decimal  Close      { get; set; }
    public decimal  Volume     { get; set; }
    public decimal  Vwap       { get; set; }
    public DateTime BarDate    { get; set; }
    public DateTime UpdatedUtc { get; set; }
}

public sealed class DailyMarketSummaryDocument
{
    public string               Id         { get; set; } = "";
    public DateTime             Date       { get; set; }
    public List<DailySummaryBar> Bars      { get; set; } = [];
    public DateTime             UpdatedUtc { get; set; }
}

public sealed class DailySummaryBar
{
    public decimal  Open      { get; set; }
    public decimal  High      { get; set; }
    public decimal  Low       { get; set; }
    public decimal  Close     { get; set; }
    public decimal  Volume    { get; set; }
    public decimal  Vwap      { get; set; }
    public DateTime Timestamp { get; set; }
}