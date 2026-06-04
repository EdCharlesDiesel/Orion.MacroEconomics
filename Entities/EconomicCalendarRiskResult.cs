namespace Orion.MacroEconomics.Entities
{
    public sealed class EconomicCalendarRiskResult
    {
        public bool IsBlocked { get; set; }
        public bool IsClear { get; set; }
        public string Reason { get; set; } = "";

        public string Message => Reason;

        public static EconomicCalendarRiskResult Clear(string reason) =>
            new()
            {
                IsBlocked = false,
                IsClear = true,
                Reason = reason
            };

        public static EconomicCalendarRiskResult Block(string reason) =>
            new()
            {
                IsBlocked = true,
                IsClear = false,
                Reason = reason
            };
    }
}
