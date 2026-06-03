namespace Orion.MacroEconomics.Entities
{
    public class Scenario
    {
        public string Name { get; set; } = default!;
        public List<ScenarioShock> Shocks { get; set; } = new();
    }

    public class ScenarioShock
    {
        public string Country { get; set; } = default!;
        public string Indicator { get; set; } = default!;

        public decimal ShockValue { get; set; } // absolute or %
        public ShockType Type { get; set; }
    }

    public enum ShockType
    {
        Absolute,   // +2%
        Relative    // +10%
    }

    public class ScenarioResult
    {
        public string ScenarioName { get; set; } = default!;

        public List<CurrencyFactorScore> Factors { get; set; } = new();
        public List<FxSignal> Signals { get; set; } = new();
        public List<PortfolioPosition> Portfolio { get; set; } = new();

        public ScenarioImpact Impact { get; set; } = new();
        public string Name { get; internal set; } = string.Empty;
        public string Direction { get; internal set; } = string.Empty;
    }

    public class ScenarioImpact
    {
        public decimal ExpectedReturnChange { get; set; }
        public decimal RiskChange { get; set; }

        public List<string> KeyDrivers { get; set; } = new();
    }
}
