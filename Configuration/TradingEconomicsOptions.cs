namespace Orion.MacroEconomics.Configuration
{
    public sealed class TradingEconomicsOptions
    {
        public string ApiKey { get; set; } = "";
        public string BaseUrl { get; set; } = "https://api.tradingeconomics.com";
        public int TimeoutSeconds { get; set; } = 30;
    }
}