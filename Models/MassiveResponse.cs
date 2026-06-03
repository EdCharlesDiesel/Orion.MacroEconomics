using System.Text.Json.Serialization;

namespace Orion.MacroEconomics.Models;

public sealed class MassiveResponse<T>
{
    [JsonPropertyName("results")]    public T?     Results   { get; set; }
    [JsonPropertyName("status")]     public string Status    { get; set; } = "";
    [JsonPropertyName("count")]      public int    Count     { get; set; }
    [JsonPropertyName("next_url")]   public string? NextUrl  { get; set; }
    [JsonPropertyName("request_id")] public string? RequestId { get; set; }
}

// ── Tickers ───────────────────────────────────────────────────────────────────

public sealed class ForexTicker
{
    [JsonPropertyName("ticker")]          public string  Ticker       { get; set; } = "";
    [JsonPropertyName("name")]            public string  Name         { get; set; } = "";
    [JsonPropertyName("market")]          public string  Market       { get; set; } = "";
    [JsonPropertyName("locale")]          public string  Locale       { get; set; } = "";
    [JsonPropertyName("base_currency")]   public string  BaseCurrency { get; set; } = "";
    [JsonPropertyName("currency_name")]   public string  CurrencyName { get; set; } = "";
    [JsonPropertyName("active")]          public bool    Active       { get; set; }
    [JsonPropertyName("last_updated_utc")] public string? LastUpdated { get; set; }
    public string QuoteCurrency { get; set; } = string.Empty;
}

public sealed class TickerDetails
{
    [JsonPropertyName("ticker")]       public string  Ticker      { get; set; } = "";
    [JsonPropertyName("name")]         public string  Name        { get; set; } = "";
    [JsonPropertyName("market")]       public string  Market      { get; set; } = "";
    [JsonPropertyName("active")]       public bool    Active      { get; set; }
    [JsonPropertyName("currency_name")] public string CurrencyName { get; set; } = "";
    [JsonPropertyName("description")]  public string? Description  { get; set; }
}

// ── Currency Conversion ───────────────────────────────────────────────────────

public sealed class ConversionResult
{
    [JsonPropertyName("from")]         public string  From          { get; set; } = "";
    [JsonPropertyName("to")]           public string  To            { get; set; } = "";
    [JsonPropertyName("converted")]    public decimal Converted     { get; set; }
    [JsonPropertyName("initial_amount")] public decimal InitialAmount { get; set; }
    [JsonPropertyName("last")]         public ConversionQuote? Last { get; set; }
    public string Timestamp { get; set; } = string.Empty;
}

public sealed class ConversionQuote
{
    [JsonPropertyName("ask")]       public decimal Ask      { get; set; }
    [JsonPropertyName("bid")]       public decimal Bid      { get; set; }
    [JsonPropertyName("exchange")]  public int     Exchange { get; set; }
    [JsonPropertyName("timestamp")] public long    Timestamp { get; set; }
}

// ── Aggregate Bars (OHLC) ─────────────────────────────────────────────────────

public sealed class AggBar
{
    [JsonPropertyName("o")]  public decimal Open      { get; set; }
    [JsonPropertyName("h")]  public decimal High      { get; set; }
    [JsonPropertyName("l")]  public decimal Low       { get; set; }
    [JsonPropertyName("c")]  public decimal Close     { get; set; }
    [JsonPropertyName("v")]  public decimal Volume    { get; set; }
    [JsonPropertyName("vw")] public decimal Vwap      { get; set; }
    [JsonPropertyName("t")]  public long    Timestamp { get; set; }
    [JsonPropertyName("n")]  public int     Trades    { get; set; }

    public DateTime TimestampUtc =>
        DateTimeOffset.FromUnixTimeMilliseconds(Timestamp).UtcDateTime;
}

public sealed class AggResponse
{
    [JsonPropertyName("ticker")]        public string       Ticker      { get; set; } = "";
    [JsonPropertyName("results")]       public List<AggBar>? Results    { get; set; }
    [JsonPropertyName("resultsCount")]  public int          ResultCount { get; set; }
    [JsonPropertyName("adjusted")]      public bool         Adjusted    { get; set; }
    [JsonPropertyName("status")]        public string       Status      { get; set; } = "";
    [JsonPropertyName("queryCount")]    public int          QueryCount  { get; set; }
}

// ── Snapshot ──────────────────────────────────────────────────────────────────

public sealed class ForexSnapshot
{
    [JsonPropertyName("ticker")]    public string         Ticker    { get; set; } = "";
    [JsonPropertyName("day")]       public SnapshotBar?   Day       { get; set; }
    [JsonPropertyName("prevDay")]   public SnapshotBar?   PrevDay   { get; set; }
    [JsonPropertyName("min")]       public SnapshotBar?   Min       { get; set; }
    [JsonPropertyName("lastQuote")] public SnapshotQuote? LastQuote { get; set; }
    [JsonPropertyName("todaysChangePerc")] public decimal TodaysChangePct { get; set; }
    [JsonPropertyName("todaysChange")]     public decimal TodaysChange    { get; set; }
    [JsonPropertyName("updated")]          public long    Updated         { get; set; }
}

public sealed class SnapshotBar
{
    [JsonPropertyName("o")]  public decimal Open   { get; set; }
    [JsonPropertyName("h")]  public decimal High   { get; set; }
    [JsonPropertyName("l")]  public decimal Low    { get; set; }
    [JsonPropertyName("c")]  public decimal Close  { get; set; }
    [JsonPropertyName("v")]  public decimal Volume { get; set; }
    [JsonPropertyName("vw")] public decimal Vwap   { get; set; }
    [JsonPropertyName("t")]  public long    Time   { get; set; }
}

public sealed class SnapshotQuote
{
    [JsonPropertyName("a")]  public decimal Ask       { get; set; }
    [JsonPropertyName("b")]  public decimal Bid       { get; set; }
    [JsonPropertyName("x")]  public int     Exchange  { get; set; }
    [JsonPropertyName("t")]  public long    Timestamp { get; set; }
}

public sealed class SnapshotResponse
{
    [JsonPropertyName("tickers")] public List<ForexSnapshot>? Tickers { get; set; }
    [JsonPropertyName("status")]  public string Status { get; set; } = "";
}

// ── Quotes ────────────────────────────────────────────────────────────────────

public sealed class ForexQuote
{
    [JsonPropertyName("ask_price")]       public decimal AskPrice      { get; set; }
    [JsonPropertyName("bid_price")]       public decimal BidPrice      { get; set; }
    [JsonPropertyName("ask_exchange")]    public int     AskExchange   { get; set; }
    [JsonPropertyName("bid_exchange")]    public int     BidExchange   { get; set; }
    [JsonPropertyName("sip_timestamp")]   public long    SipTimestamp  { get; set; }
    [JsonPropertyName("participant_timestamp")] public long ParticipantTimestamp { get; set; }
    public string SequenceNumber { get; set; } = string.Empty;
}

public sealed class LastQuoteResult
{
    [JsonPropertyName("ask")]       public decimal Ask      { get; set; }
    [JsonPropertyName("bid")]       public decimal Bid      { get; set; }
    [JsonPropertyName("exchange")]  public int     Exchange { get; set; }
    [JsonPropertyName("timestamp")] public long    Timestamp { get; set; }
}


public sealed class IndicatorResponse
{
    public List<IndicatorValue> Results { get; set; } = new();
    public string Status { get; set; } = "";
    public string? RequestId { get; set; }
    public string? NextUrl { get; set; }
}

public sealed class IndicatorResults
{
    [JsonPropertyName("values")]     public List<IndicatorValue>? Values     { get; set; }
    [JsonPropertyName("underlying")] public IndicatorUnderlying?  Underlying { get; set; }
}

public sealed class IndicatorValue
{
    [JsonPropertyName("timestamp")] public long    Timestamp { get; set; }
    [JsonPropertyName("value")]     public decimal Value     { get; set; }

    public DateTime TimestampUtc =>
        DateTimeOffset.FromUnixTimeMilliseconds(Timestamp).UtcDateTime;
}

public sealed class IndicatorUnderlying
{
    [JsonPropertyName("url")]        public string?        Url     { get; set; }
    [JsonPropertyName("aggregates")] public List<AggBar>?  Bars    { get; set; }
}

public sealed class MacdResponse
{
    [JsonPropertyName("results")]    public MacdResults? Results   { get; set; }
    [JsonPropertyName("status")]     public string       Status    { get; set; } = "";
    [JsonPropertyName("request_id")] public string?      RequestId { get; set; }
}

public sealed class MacdResults
{
    [JsonPropertyName("values")]     public List<MacdValue>?       Values     { get; set; }
    [JsonPropertyName("underlying")] public IndicatorUnderlying?   Underlying { get; set; }
}

public sealed class MacdValue
{
    [JsonPropertyName("timestamp")]  public long    Timestamp  { get; set; }
    [JsonPropertyName("value")]      public decimal Value      { get; set; }
    [JsonPropertyName("signal")]     public decimal Signal     { get; set; }
    [JsonPropertyName("histogram")]  public decimal Histogram  { get; set; }

    public DateTime TimestampUtc =>
        DateTimeOffset.FromUnixTimeMilliseconds(Timestamp).UtcDateTime;
}

// ── Market Operations ─────────────────────────────────────────────────────────

public sealed class Exchange
{
    [JsonPropertyName("id")]           public int    Id          { get; set; }
    [JsonPropertyName("name")]         public string Name        { get; set; } = "";
    [JsonPropertyName("type")]         public string Type        { get; set; } = "";
    [JsonPropertyName("asset_class")]  public string AssetClass  { get; set; } = "";
    [JsonPropertyName("locale")]       public string Locale      { get; set; } = "";
    [JsonPropertyName("acronym")]      public string? Acronym    { get; set; }
    [JsonPropertyName("operating_mic")] public string? OperatingMic { get; set; }
    [JsonPropertyName("mic")]          public string? Mic         { get; set; }
    [JsonPropertyName("url")]          public string? Url         { get; set; }
}

public sealed class MarketHoliday
{
    [JsonPropertyName("exchange")]  public string  Exchange { get; set; } = "";
    [JsonPropertyName("name")]      public string  Name     { get; set; } = "";
    [JsonPropertyName("status")]    public string  Status   { get; set; } = "";
    [JsonPropertyName("date")]      public DateTime  HolidayDate     { get; set; }
    [JsonPropertyName("open")]      public string? Open     { get; set; }
    [JsonPropertyName("close")]     public string? Close    { get; set; }
}

public sealed class MarketStatus
{
    [JsonPropertyName("market")]       public string  Market      { get; set; } = "";
    [JsonPropertyName("serverTime")]   public string  ServerTime  { get; set; } = "";
    [JsonPropertyName("exchanges")]    public MarketExchangeStatus? Exchanges { get; set; }
    [JsonPropertyName("currencies")]   public MarketCurrencyStatus? Currencies { get; set; }
}

public sealed class MarketExchangeStatus
{
    [JsonPropertyName("nyse")]   public string? Nyse   { get; set; }
    [JsonPropertyName("nasdaq")] public string? Nasdaq { get; set; }
    [JsonPropertyName("otc")]    public string? Otc    { get; set; }
}

public sealed class MarketCurrencyStatus
{
    [JsonPropertyName("fx")]     public string? Fx     { get; set; }
    [JsonPropertyName("crypto")] public string? Crypto { get; set; }
}