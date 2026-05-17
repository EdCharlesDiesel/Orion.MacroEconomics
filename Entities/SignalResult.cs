using Orion.MacroEconomics.Enum;

namespace Orion.MacroEconomics.Entities
{
    public sealed class SignalResult
    {
        public string Pair { get; set; } = "";
        public string Direction { get; set; } = "NO_TRADE";
        public decimal Confidence { get; set; }
        public decimal Score { get; set; }
        public string Reason { get; set; } = "";

        public static SignalResult NoTrade(string reason)
        {
            return new SignalResult
            {
                Direction = "NO_TRADE",
                Confidence = 0,
                Score = 0,
                Reason = reason
            };
        }
    }

    public sealed class NormalizedMarketContext
    {
        public string Pair { get; set; } = "";
        public decimal Spread { get; set; }
        public List<OhlcvBar> Candles { get; set; } = new();
    }





}
