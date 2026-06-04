namespace Orion.MacroEconomics.Entities
{
    public sealed class PortfolioRiskResult
    {
        public bool IsAllowed { get; set; }
        public string Reason { get; set; } = "";

        /// <summary>
        /// Convenience alias for <see cref="IsAllowed"/> so both naming conventions
        /// stay in sync. (Previously this was a separate, never-assigned field.)
        /// </summary>
        public bool Allowed => IsAllowed;

        public static PortfolioRiskResult Allow(string reason)
        {
            return new PortfolioRiskResult
            {
                IsAllowed = true,
                Reason = reason
            };
        }

        public static PortfolioRiskResult Block(string reason)
        {
            return new PortfolioRiskResult
            {
                IsAllowed = false,
                Reason = reason
            };
        }
    }
}
