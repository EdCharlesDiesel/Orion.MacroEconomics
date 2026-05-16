using Orion.MacroEconomics.Entities;
using Orion.MacroEconomics.Models;

namespace Orion.MacroEconomics.DTO;

public sealed class ScenarioBuildRequest
{
    public NormalizedIndicator Normalized { get; set; } = new();
    public RegimeResult Regime { get; set; } = new();
}

public sealed class ProbabilisticScenarioRequest
{
    public NormalizedIndicator Normalized { get; set; } = new();
    public RegimeResult Regime { get; set; } = new();
    public ScenarioResult Scenario { get; set; } = new();
}

public sealed class MacroSimulationRequest
{
    public NormalizedIndicator Normalized { get; set; } = new();
    public RegimeResult Regime { get; set; } = new();
    public ProbabilisticScenarioResult Probabilities { get; set; } = new();
}

public sealed class SignalRequest
{
    public NormalizedMarketContext Market { get; set; } = new();
    public RegimeResult Regime { get; set; } = new();
    public ScenarioResult Scenario { get; set; } = new();
    public ProbabilisticScenarioResult Probabilities { get; set; } = new();
    public MacroSimulationResult MacroSimulation { get; set; } = new();
}

public sealed class RiskRequest
{
    public SignalResult Signal { get; set; } = new();
    public NormalizedMarketContext Market { get; set; } = new();
    public RegimeResult Regime { get; set; } = new();
}

public sealed class PositionSizingRequest
{
    public SignalResult Signal { get; set; } = new();
    public RiskResult Risk { get; set; } = new();
    public NormalizedMarketContext Market { get; set; } = new();
    public AccountContext Account { get; set; } = new();
}

public sealed class FxPricingRequest
{
    public string Pair { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty;
    public decimal PositionSize { get; set; }
}

public sealed class FxSimulationRequest
{
    public List<MacroState> States { get; set; } = [];
    public Dictionary<string, decimal> InitialPrices { get; set; } = [];
}

public sealed class LiveExecutionRequest
{
    public string Pair { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty;
    public decimal Size { get; set; }
}

public sealed class OrderBookExecutionRequest
{
    public OrderBook OrderBook { get; set; } = new();
    public string Direction { get; set; } = string.Empty;
    public decimal Size { get; set; }
}

public sealed class ExitRequest
{
    public SignalResult Signal { get; set; } = new();
    public ExecutionOrder Execution { get; set; } = new();
    public RiskResult Risk { get; set; } = new();
    public List<NormalizedIndicator>? Market { get; set; } = new();
}

public sealed class TradeLifecycleCreateRequest
{
    public SignalResult Signal { get; set; } = new();
    public RiskResult Risk { get; set; } = new();
    public PositionSizeResult Size { get; set; } = new();
    public ExecutionOrder Execution { get; set; } = new();
    public ExitPlan Exit { get; set; } = new();
}

public sealed class TradeLifecycleUpdateRequest
{
    public TradePlan Trade { get; set; } = new();
    public OhlcvBar LatestCandle { get; set; } = new();
}

public sealed class ModelValidationRequest
{
    public PerformanceReport Performance { get; set; } = new();
    public List<TradePlan> Trades { get; set; } = [];
}

public sealed class PortfolioRiskRequest
{
    public TradePlan NewTrade { get; set; } = new();
    public List<TradePlan> OpenTrades { get; set; } = [];
    public AccountContext Account { get; set; } = new();
}

public sealed class OrderCreateRequest
{
    public TradePlan Trade { get; set; } = new();
    public PositionSizeResult Size { get; set; } = new();
    public AccountContext Account { get; set; } = new();
}

public sealed class OrderFillValidationRequest
{
    public OrderRequest Order { get; set; } = new();
    public ExecutionOrder Execution { get; set; } = new();
}

public sealed class OrderCancelRequest
{
    public OrderRequest Order { get; set; } = new();
    public string Reason { get; set; } = "Order cancelled.";
}

public sealed class RealTimeRiskRequest
{
    public AccountSnapshot Account { get; set; } = new();
    public ExecutionOrder Execution { get; set; } = new();
    public ExitPlan ExitPlan { get; set; } = new();
    public MarketQuote Quote { get; set; } = new();
}