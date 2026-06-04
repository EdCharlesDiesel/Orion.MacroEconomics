namespace Orion.MacroEconomics.Entities
{
    public sealed class TradePlan
    {
        public Guid Id { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Pair { get; set; } = "";
        public string Direction { get; set; } = "";
        public decimal EntryPrice { get; set; }
        public decimal PositionSize { get; set; }
        public decimal StopLoss { get; set; }
        public decimal TakeProfit { get; set; }
        public decimal ClosePrice { get; set; }
        public decimal ProfitLoss { get; set; }
        public DateTime OpenedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public string CloseReason { get; set; } = "";
        public string Reason { get; set; } = "";
        public decimal ExitPrice { get; set; }
        public decimal TakeProfit1 { get; set; }
        public decimal TakeProfit2 { get; set; }
        public string Timeframe { get; set; } = string.Empty;
        public string Reasoning { get; set; } = string.Empty;
        public decimal RiskReward { get; set; }
        public decimal ATR { get; set; }
        public decimal EMA20 { get; set; }
        public decimal EMA50 { get; set; }
        public decimal Support { get; set; }
        public decimal Resistance { get; set; }
        public decimal RSI { get; set; }
        public decimal PnL { get; set; }

        public static TradePlan Rejected(string reason)
        {
            return new TradePlan
            {
                Id = Guid.NewGuid(),
                Status = "REJECTED",
                Reason = reason
            };
        }

        public TradePlan Close(string reason, decimal closePrice, DateTime closedAt)
        {
            Status = "CLOSED";
            CloseReason = reason;
            ClosePrice = closePrice;
            ExitPrice = closePrice;
            ClosedAt = closedAt;

            ProfitLoss = Direction == "LONG"
                ? (closePrice - EntryPrice) * PositionSize
                : (EntryPrice - closePrice) * PositionSize;

            return this;
        }
    }
}
