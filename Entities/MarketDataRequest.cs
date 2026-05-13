namespace Orion.MacroEconomics.Entities
{
    public sealed class MarketDataRequest
    {
        public string Pair { get; set; } = "";
        public string Timeframe { get; set; } = "1h";
        public DateTime FromUtc { get; set; }
        public DateTime ToUtc { get; set; }
        public string Provider { get; set; } = "Yahoo";
        public string? Interval { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
    }
}
