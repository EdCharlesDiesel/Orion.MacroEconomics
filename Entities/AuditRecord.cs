using Orion.MacroEconomics.DTO;
using Orion.MacroEconomics.Models;
namespace Orion.MacroEconomics.Entities;
public class AuditRecord
{
    public string Stage { get; set; } = "";
    public string Status { get; set; } = "";
    public string Pair { get; set; } = "";
    public string Reason { get; set; } = "";
    public string PayloadJson { get; set; } = "";
    public DateTime TimestampUtc { get; set; }
    public Guid CorrelationId { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public string TraderId { get; set; } = string.Empty;
    public DateTime ExecutionTime { get; set; }
    public ForexMarketInput Input { get; set; } = new();
    public NormalizedMarketContext NormalizedContext { get; set; } = new();
    public RegimeResult Regime { get; set; } = new();
    public ScenarioResult Scenario { get; set; } = new();
    public ProbabilisticResult Probabilities { get; set; } = new();
    public MacroSimulationResult MacroSimulation { get; set; } = new();
    public SignalResult Signal { get; set; } = new();
    public RiskEvaluation Risk { get; set; } = new();
    public decimal PositionSize { get; set; }
    public ExecutionResult Execution { get; set; } = new();
    public ExitStrategy Exit { get; set; } = new();
    public TradingDecision Decision { get; set; } = new();
    public TimeSpan TotalProcessingTime { get; set; }
    public Dictionary<string, TimeSpan> StepTimings { get; set; } = new();
    public string Version { get; set; } = string.Empty;
    public Dictionary<string, object> Tags { get; set; } = new();
}
