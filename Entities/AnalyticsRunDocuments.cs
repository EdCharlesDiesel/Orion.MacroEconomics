namespace Orion.MacroEconomics.Entities;

/// <summary>
/// Marten document wrapping a single backtest execution: request parameters + the trade list produced.
/// </summary>
public sealed class BacktestRunDocument
{
    public Guid     Id        { get; set; } = Guid.NewGuid();
    public DateTime RunAt     { get; set; } = DateTime.UtcNow;
    public DateTime Start     { get; set; }
    public DateTime End       { get; set; }
    public decimal  Capital   { get; set; }
    public int      TradeCount{ get; set; }
    public List<TradeResult> Trades { get; set; } = new();
}

/// <summary>
/// Marten document wrapping a walk-forward analysis run.
/// </summary>
public sealed class WalkForwardRunDocument
{
    public Guid     Id       { get; set; } = Guid.NewGuid();
    public DateTime RunAt    { get; set; } = DateTime.UtcNow;
    public DateTime Start    { get; set; }
    public DateTime End      { get; set; }
    public int      SegmentCount { get; set; }
    public List<WalkForwardResult> Segments { get; set; } = new();
}

/// <summary>
/// Marten document wrapping a Monte-Carlo run: input trades, simulation count, and final equity distribution.
/// </summary>
public sealed class MonteCarloRunDocument
{
    public Guid     Id              { get; set; } = Guid.NewGuid();
    public DateTime RunAt           { get; set; } = DateTime.UtcNow;
    public int      Simulations     { get; set; }
    public int      InputTradeCount { get; set; }
    public List<decimal> FinalEquities { get; set; } = new();
}

/// <summary>
/// Marten document wrapping a performance-analytics report computed from a set of trade plans.
/// </summary>
public sealed class PerformanceReportDocument
{
    public Guid     Id           { get; set; } = Guid.NewGuid();
    public DateTime RunAt        { get; set; } = DateTime.UtcNow;
    public int      InputTrades  { get; set; }
    public PerformanceReport Report { get; set; } = new();
}

/// <summary>
/// Marten document wrapping a single real-time risk decision against a live execution.
/// </summary>
public sealed class RealTimeRiskRunDocument
{
    public Guid     Id     { get; set; } = Guid.NewGuid();
    public DateTime RunAt  { get; set; } = DateTime.UtcNow;
    public string   Pair   { get; set; } = string.Empty;
    public RealTimeRiskResult Result { get; set; } = new();
}

/// <summary>
/// Marten document wrapping a circuit-breaker evaluation.
/// </summary>
public sealed class CircuitBreakerRunDocument
{
    public Guid     Id    { get; set; } = Guid.NewGuid();
    public DateTime RunAt { get; set; } = DateTime.UtcNow;
    public int      TodayTradeCount { get; set; }
    public int      OpenTradeCount  { get; set; }
    public CircuitBreakerResult Result { get; set; } = new();
}

/// <summary>
/// Marten document wrapping a compliance validation against a proposed trade.
/// </summary>
public sealed class ComplianceRunDocument
{
    public Guid     Id            { get; set; } = Guid.NewGuid();
    public DateTime RunAt         { get; set; } = DateTime.UtcNow;
    public string   Pair          { get; set; } = string.Empty;
    public string   Direction     { get; set; } = string.Empty;
    public decimal  RequestedSize { get; set; }
    public ComplianceResult Result { get; set; } = new();
}

/// <summary>
/// Marten document wrapping an economic-calendar risk evaluation.
/// </summary>
public sealed class EconomicCalendarRiskRunDocument
{
    public Guid     Id          { get; set; } = Guid.NewGuid();
    public DateTime RunAt       { get; set; } = DateTime.UtcNow;
    public string   Pair        { get; set; } = string.Empty;
    public DateTime EvaluatedAt { get; set; }
    public EconomicCalendarRiskResult Result { get; set; } = new();
}

/// <summary>Persists every regime detection / analysis result.</summary>
public sealed class RegimeRunDocument
{
    public Guid     Id     { get; set; } = Guid.NewGuid();
    public DateTime RunAt  { get; set; } = DateTime.UtcNow;
    public string   Source { get; set; } = string.Empty; // "Detect" | "Next" | "Analyze"
    public DTO.RegimeResult Result { get; set; } = new();
}

/// <summary>Persists every scenario engine run / build.</summary>
public sealed class ScenarioRunDocument
{
    public Guid     Id     { get; set; } = Guid.NewGuid();
    public DateTime RunAt  { get; set; } = DateTime.UtcNow;
    public string   Source { get; set; } = string.Empty; // "Run" | "Build"
    public string   ScenarioName { get; set; } = string.Empty;
    public ScenarioResult Result { get; set; } = new();
}

/// <summary>Persists every probabilistic scenario calculation.</summary>
public sealed class ProbabilisticScenarioRunDocument
{
    public Guid     Id    { get; set; } = Guid.NewGuid();
    public DateTime RunAt { get; set; } = DateTime.UtcNow;
    public ProbabilisticScenarioResult Result { get; set; } = new();
}

/// <summary>Persists every macro forward-simulation result.</summary>
public sealed class MacroSimulationRunDocument
{
    public Guid     Id    { get; set; } = Guid.NewGuid();
    public DateTime RunAt { get; set; } = DateTime.UtcNow;
    public string   Source { get; set; } = string.Empty; // "Simulate" | "Run"
    public Models.MacroSimulationResult Result { get; set; } = new();
}

/// <summary>Persists every directional signal the system generates.</summary>
public sealed class SignalRunDocument
{
    public Guid     Id     { get; set; } = Guid.NewGuid();
    public DateTime RunAt  { get; set; } = DateTime.UtcNow;
    public string   Pair   { get; set; } = string.Empty;
    public SignalResult Result { get; set; } = new();
}

/// <summary>Persists every pre-trade risk gate evaluation.</summary>
public sealed class RiskEvaluationRunDocument
{
    public Guid     Id        { get; set; } = Guid.NewGuid();
    public DateTime RunAt     { get; set; } = DateTime.UtcNow;
    public string   Pair      { get; set; } = string.Empty;
    public string   Direction { get; set; } = string.Empty;
    public RiskResult Result { get; set; } = new();
}

/// <summary>Persists every correlation snapshot.</summary>
public sealed class CorrelationRunDocument
{
    public Guid     Id          { get; set; } = Guid.NewGuid();
    public DateTime RunAt       { get; set; } = DateTime.UtcNow;
    public string   PrimaryPair { get; set; } = string.Empty;
    public CorrelationResult Result { get; set; } = new();
}

/// <summary>Persists every liquidity / depth analysis.</summary>
public sealed class LiquidityRunDocument
{
    public Guid     Id    { get; set; } = Guid.NewGuid();
    public DateTime RunAt { get; set; } = DateTime.UtcNow;
    public string   Pair  { get; set; } = string.Empty;
    public LiquidityResult Result { get; set; } = new();
}

/// <summary>Persists every hedging analysis / recommendation set.</summary>
public sealed class HedgingRunDocument
{
    public Guid     Id    { get; set; } = Guid.NewGuid();
    public DateTime RunAt { get; set; } = DateTime.UtcNow;
    public string   BaseCurrency { get; set; } = string.Empty;
    public HedgingResult Result { get; set; } = new();
}

/// <summary>Persists every sentiment analysis snapshot.</summary>
public sealed class SentimentRunDocument
{
    public Guid     Id    { get; set; } = Guid.NewGuid();
    public DateTime RunAt { get; set; } = DateTime.UtcNow;
    public string   Pair  { get; set; } = string.Empty;
    public SentimentResult Result { get; set; } = new();
}

/// <summary>Persists every model-validation report.</summary>
public sealed class ModelValidationRunDocument
{
    public Guid     Id          { get; set; } = Guid.NewGuid();
    public DateTime RunAt       { get; set; } = DateTime.UtcNow;
    public int      InputTrades { get; set; }
    public ModelValidationReport Result { get; set; } = new();
}

/// <summary>Persists every portfolio-risk gating decision.</summary>
public sealed class PortfolioRiskRunDocument
{
    public Guid     Id              { get; set; } = Guid.NewGuid();
    public DateTime RunAt           { get; set; } = DateTime.UtcNow;
    public int      OpenTradeCount  { get; set; }
    public PortfolioRiskResult Result { get; set; } = new();
}

/// <summary>Persists every full live-trading orchestration outcome.</summary>
public sealed class LiveTradingRunDocument
{
    public Guid     Id     { get; set; } = Guid.NewGuid();
    public DateTime RunAt  { get; set; } = DateTime.UtcNow;
    public string   Pair   { get; set; } = string.Empty;
    public LiveTradingResult Result { get; set; } = new();
}
