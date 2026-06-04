using Marten;
using Microsoft.AspNetCore.Mvc;
using Orion.MacroEconomics.DTO;
using Orion.MacroEconomics.Engine.Interfaces;
using Orion.MacroEconomics.Engine.Interfaces.Orion.API.TradingEconomics.Engine.Interfaces;
using Orion.MacroEconomics.Entities;
using Orion.MacroEconomics.Enum;
using Orion.MacroEconomics.Models;

namespace Orion.MacroEconomics.Controllers;

[ApiController]
[Route("api/trading-engines")]
public sealed class TradingEnginesController(
    IDataQualityEngine dataQuality,
    INormalizationEngine normalization,
    IRegimeEngine regime,
    IScenarioEngine scenario,
    IProbabilisticScenarioEngine probabilisticScenario,
    IMacroSimulationEngine macroSimulation,
    ISignalEngine signal,
    IRiskEngine risk,
    IPositionSizingEngine positionSizing,
    IFxPricingEngine fxPricing,
    IExecutionEngine execution,
    IExitEngine exit,
    ITradeLifecycleEngine tradeLifecycle,
    ICorrelationEngine correlation,
    ILiquidityEngine liquidity,
    IHedgingEngine hedging,
    ISentimentEngine sentiment,
    IPerformanceAnalyticsEngine performance,
    IModelValidationEngine modelValidation,
    IPortfolioEngine portfolio,
    IOrderManagementEngine orderManagement,
    IRealTimeRiskEngine realTimeRisk,
    IMarketDataEngine marketData,
    IDocumentSession session)
    : ControllerBase
{
    [HttpPost("data-quality/validate")]
    public ActionResult<DataQualityResult> ValidateDataQuality(ForexMarketInput input)
        => Ok(dataQuality.ValidateCandles(input.Candles));

    [HttpPost("normalization/normalize")]
    public ActionResult<List<NormalizedIndicator>> Normalize(List<EconomicIndicator> indicators)
        => Ok(normalization.Normalize(indicators));

    [HttpPost("regime/detect")]
    public async Task<ActionResult<RegimeResult>> DetectRegime(NormalizedIndicator indicator, CancellationToken ct)
    {
        var result = regime.Detect(indicator);
        session.Store(new RegimeRunDocument { Source = "Detect", Result = result });
        await session.SaveChangesAsync(ct);
        return Ok(result);
    }

    [HttpGet("regime/next")]
    public async Task<ActionResult<RegimeResult>> NextRegime([FromQuery] MarketRegime current, CancellationToken ct)
    {
        var result = regime.Next(current);
        session.Store(new RegimeRunDocument { Source = "Next", Result = result });
        await session.SaveChangesAsync(ct);
        return Ok(result);
    }

    [HttpPost("scenario/run")]
    public async Task<ActionResult<ScenarioResult>> RunScenario(
        Scenario request,
        CancellationToken cancellationToken)
    {
        var result = await scenario.RunAsync(request, cancellationToken);
        session.Store(new ScenarioRunDocument
        {
            Source       = "Run",
            ScenarioName = result?.ScenarioName ?? result?.Name ?? request?.Name ?? string.Empty,
            Result       = result ?? new ScenarioResult()
        });
        await session.SaveChangesAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost("scenario/build")]
    public async Task<ActionResult<ScenarioResult>> BuildScenario(ScenarioBuildRequest request, CancellationToken ct)
    {
        var result = scenario.Build(request.Normalized, request.Regime);
        session.Store(new ScenarioRunDocument
        {
            Source       = "Build",
            ScenarioName = result?.ScenarioName ?? result?.Name ?? string.Empty,
            Result       = result ?? new ScenarioResult()
        });
        await session.SaveChangesAsync(ct);
        return Ok(result);
    }

    [HttpPost("probabilistic-scenario/calculate")]
    public async Task<ActionResult<ProbabilisticScenarioResult>> CalculateProbabilisticScenario(
        ProbabilisticScenarioRequest request,
        CancellationToken ct)
    {
        var result = probabilisticScenario.Calculate(
            request.Normalized,
            request.Regime,
            request.Scenario);
        session.Store(new ProbabilisticScenarioRunDocument { Result = result ?? new ProbabilisticScenarioResult() });
        await session.SaveChangesAsync(ct);
        return Ok(result);
    }

    [HttpPost("macro-simulation/simulate")]
    public async Task<ActionResult<MacroSimulationResult>> SimulateMacro(MacroSimulationRequest request, CancellationToken ct)
    {
        var result = macroSimulation.Simulate(
            request.Normalized,
            request.Regime,
            request.Probabilities);
        session.Store(new MacroSimulationRunDocument { Source = "Simulate", Result = result ?? new MacroSimulationResult() });
        await session.SaveChangesAsync(ct);
        return Ok(result);
    }

    [HttpPost("signal/generate")]
    public async Task<ActionResult<SignalResult>> GenerateSignal(SignalRequest request, CancellationToken ct)
    {
        var result = signal.Generate(
            request.Market,
            request.Regime,
            request.Scenario,
            request.Probabilities,
            request.MacroSimulation);
        session.Store(new SignalRunDocument
        {
            Pair   = result?.Pair ?? request.Market?.Pair ?? string.Empty,
            Result = result ?? new SignalResult()
        });
        await session.SaveChangesAsync(ct);
        return Ok(result);
    }

    [HttpPost("risk/evaluate")]
    public async Task<ActionResult<RiskResult>> EvaluateRisk(RiskRequest request, CancellationToken ct)
    {
        var result = risk.Evaluate(
            request.Signal,
            request.Market,
            request.Regime);
        session.Store(new RiskEvaluationRunDocument
        {
            Pair      = request.Signal?.Pair ?? request.Market?.Pair ?? string.Empty,
            Direction = request.Signal?.Direction ?? string.Empty,
            Result    = result ?? new RiskResult()
        });
        await session.SaveChangesAsync(ct);
        return Ok(result);
    }

    [HttpPost("position-sizing/calculate")]
    public ActionResult<PositionSizeResult> CalculatePositionSize(PositionSizingRequest request)
        => Ok(positionSizing.Calculate(
            request.Signal,
            request.Risk,
            request.Market,
            request.Account));

    [HttpPost("fx-pricing/price")]
    public ActionResult<PricingResult> PriceFx(FxPricingRequest request)
        => Ok(fxPricing.Price(
            request.Pair,
            request.Direction,
            request.PositionSize));

    [HttpPost("fx-pricing/simulate")]
    public ActionResult<List<FxPrice>> SimulateFxPrices(FxSimulationRequest request)
        => Ok(fxPricing.Run(
            request.States,
            request.InitialPrices));

    [HttpPost("execution/live")]
    public async Task<ActionResult<ExecutionOrder>> ExecuteLive(
        LiveExecutionRequest request,
        CancellationToken cancellationToken)
        => Ok(await execution.ExecuteAsync(
            request.Pair,
            request.Direction,
            request.Size,
            cancellationToken));

    [HttpPost("execution/order-book")]
    public ActionResult<ExecutionOrder> ExecuteOrderBook(OrderBookExecutionRequest request)
        => Ok(execution.Execute(
            request.OrderBook,
            request.Direction,
            request.Size));

    [HttpPost("exit/calculate")]
    public ActionResult<ExitPlan> CalculateExit(ExitRequest request)
        => Ok(exit.Calculate(
            request.Signal,
            request.Execution,
            request.Risk,
            request.Market));

    [HttpPost("trade-lifecycle/create")]
    public ActionResult<TradePlan> CreateTradePlan(TradeLifecycleCreateRequest request)
        => Ok(tradeLifecycle.CreatePlan(
            request.Signal,
            request.Risk,
            request.Size,
            request.Execution,
            request.Exit));

    [HttpPost("trade-lifecycle/update")]
    public ActionResult<TradePlan> UpdateTradePlan(TradeLifecycleUpdateRequest request)
        => Ok(tradeLifecycle.Update(
            request.Trade,
            request.LatestCandle));

    [HttpPost("correlation/analyze")]
    public async Task<ActionResult<CorrelationResult>> AnalyzeCorrelation(
        CorrelationRequest request,
        CancellationToken cancellationToken)
    {
        var result = await correlation.AnalyzeAsync(request, cancellationToken);
        session.Store(new CorrelationRunDocument
        {
            PrimaryPair = result?.PrimaryPair ?? request.PrimaryPair ?? string.Empty,
            Result      = result ?? new CorrelationResult()
        });
        await session.SaveChangesAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost("liquidity/analyze")]
    public async Task<ActionResult<LiquidityResult>> AnalyzeLiquidity(
        LiquidityRequest request,
        CancellationToken cancellationToken)
    {
        var result = await liquidity.AnalyzeAsync(request, cancellationToken);
        session.Store(new LiquidityRunDocument
        {
            Pair   = result?.Pair ?? request.Pair ?? string.Empty,
            Result = result ?? new LiquidityResult()
        });
        await session.SaveChangesAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost("hedging/analyze")]
    public async Task<ActionResult<HedgingResult>> AnalyzeHedging(
        HedgingRequest request,
        CancellationToken cancellationToken)
    {
        var result = await hedging.AnalyzeAsync(request, cancellationToken);
        session.Store(new HedgingRunDocument
        {
            BaseCurrency = request.PortfolioBaseCurrency ?? string.Empty,
            Result       = result ?? new HedgingResult()
        });
        await session.SaveChangesAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost("sentiment/analyze")]
    public async Task<ActionResult<SentimentResult>> AnalyzeSentiment(
        SentimentRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sentiment.AnalyzeAsync(request, cancellationToken);
        session.Store(new SentimentRunDocument
        {
            Pair   = result?.Pair ?? request.Pair ?? string.Empty,
            Result = result ?? new SentimentResult()
        });
        await session.SaveChangesAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost("performance/analyze")]
    public ActionResult<PerformanceReport> AnalyzePerformance(List<TradePlan> trades)
        => Ok(performance.Analyze(trades));

    [HttpPost("model-validation/validate")]
    public async Task<ActionResult<ModelValidationReport>> ValidateModel(ModelValidationRequest request, CancellationToken ct)
    {
        var result = modelValidation.Validate(
            request.Performance,
            request.Trades);
        session.Store(new ModelValidationRunDocument
        {
            InputTrades = request.Trades?.Count ?? 0,
            Result      = result ?? new ModelValidationReport()
        });
        await session.SaveChangesAsync(ct);
        return Ok(result);
    }

    [HttpPost("portfolio/evaluate")]
    public async Task<ActionResult<PortfolioRiskResult>> EvaluatePortfolio(PortfolioRiskRequest request, CancellationToken ct)
    {
        var result = portfolio.Evaluate(
            request.NewTrade,
            request.OpenTrades,
            request.Account);
        session.Store(new PortfolioRiskRunDocument
        {
            OpenTradeCount = request.OpenTrades?.Count ?? 0,
            Result         = result ?? new PortfolioRiskResult()
        });
        await session.SaveChangesAsync(ct);
        return Ok(result);
    }

    [HttpPost("order-management/create")]
    public ActionResult<OrderRequest> CreateOrder(OrderCreateRequest request)
        => Ok(orderManagement.CreateOrder(
            request.Trade,
            request.Size,
            request.Account));

    [HttpPost("order-management/validate-fill")]
    public ActionResult<OrderState> ValidateOrderFill(OrderFillValidationRequest request)
        => Ok(orderManagement.ValidateFill(
            request.Order,
            request.Execution));

    [HttpPost("order-management/cancel")]
    public ActionResult<OrderState> CancelOrder(OrderCancelRequest request)
        => Ok(orderManagement.Cancel(
            request.Order,
            request.Reason));

    [HttpPost("real-time-risk/evaluate")]
    public ActionResult<RealTimeRiskResult> EvaluateRealTimeRisk(RealTimeRiskRequest request)
        => Ok(realTimeRisk.Evaluate(
            request.Account,
            request.Execution,
            request.ExitPlan,
            request.Quote));

    [HttpGet("market-data/macro")]
    public async Task<ActionResult<MacroData>> GetMacroData(CancellationToken cancellationToken)
        => Ok(await marketData.GetMacroDataAsync(cancellationToken));

    [HttpPost("market-data/refresh")]
    public async Task<ActionResult<MacroData>> RefreshMacroData(CancellationToken cancellationToken)
       => Ok(await marketData.RefreshMacroDataAsync(cancellationToken));

    [HttpGet("market-data/health")]
    public async Task<ActionResult<MarketDataHealth>> CheckMarketDataHealth(
        [FromQuery] string pair,
        CancellationToken cancellationToken)
        => Ok(await marketData.CheckHealthAsync(pair, cancellationToken));

    // ──────────────────────────────────────────────────────────────────────
    // History endpoints — most recent persisted runs for each engine output.
    // ──────────────────────────────────────────────────────────────────────

    [HttpGet("regime/runs")]
    public async Task<ActionResult<List<RegimeRunDocument>>> ListRegimeRuns([FromQuery] int limit = 50, CancellationToken ct = default) =>
        Ok(await session.Query<RegimeRunDocument>().OrderByDescending(x => x.RunAt).Take(Math.Clamp(limit, 1, 500)).ToListAsync(ct));

    [HttpGet("scenario/runs")]
    public async Task<ActionResult<List<ScenarioRunDocument>>> ListScenarioRuns([FromQuery] int limit = 50, CancellationToken ct = default) =>
        Ok(await session.Query<ScenarioRunDocument>().OrderByDescending(x => x.RunAt).Take(Math.Clamp(limit, 1, 500)).ToListAsync(ct));

    [HttpGet("probabilistic-scenario/runs")]
    public async Task<ActionResult<List<ProbabilisticScenarioRunDocument>>> ListProbScenarioRuns([FromQuery] int limit = 50, CancellationToken ct = default) =>
        Ok(await session.Query<ProbabilisticScenarioRunDocument>().OrderByDescending(x => x.RunAt).Take(Math.Clamp(limit, 1, 500)).ToListAsync(ct));

    [HttpGet("macro-simulation/runs")]
    public async Task<ActionResult<List<MacroSimulationRunDocument>>> ListMacroSimRuns([FromQuery] int limit = 50, CancellationToken ct = default) =>
        Ok(await session.Query<MacroSimulationRunDocument>().OrderByDescending(x => x.RunAt).Take(Math.Clamp(limit, 1, 500)).ToListAsync(ct));

    [HttpGet("signal/runs")]
    public async Task<ActionResult<List<SignalRunDocument>>> ListSignalRuns([FromQuery] int limit = 50, CancellationToken ct = default) =>
        Ok(await session.Query<SignalRunDocument>().OrderByDescending(x => x.RunAt).Take(Math.Clamp(limit, 1, 500)).ToListAsync(ct));

    [HttpGet("risk/runs")]
    public async Task<ActionResult<List<RiskEvaluationRunDocument>>> ListRiskRuns([FromQuery] int limit = 50, CancellationToken ct = default) =>
        Ok(await session.Query<RiskEvaluationRunDocument>().OrderByDescending(x => x.RunAt).Take(Math.Clamp(limit, 1, 500)).ToListAsync(ct));

    [HttpGet("correlation/runs")]
    public async Task<ActionResult<List<CorrelationRunDocument>>> ListCorrelationRuns([FromQuery] int limit = 50, CancellationToken ct = default) =>
        Ok(await session.Query<CorrelationRunDocument>().OrderByDescending(x => x.RunAt).Take(Math.Clamp(limit, 1, 500)).ToListAsync(ct));

    [HttpGet("liquidity/runs")]
    public async Task<ActionResult<List<LiquidityRunDocument>>> ListLiquidityRuns([FromQuery] int limit = 50, CancellationToken ct = default) =>
        Ok(await session.Query<LiquidityRunDocument>().OrderByDescending(x => x.RunAt).Take(Math.Clamp(limit, 1, 500)).ToListAsync(ct));

    [HttpGet("hedging/runs")]
    public async Task<ActionResult<List<HedgingRunDocument>>> ListHedgingRuns([FromQuery] int limit = 50, CancellationToken ct = default) =>
        Ok(await session.Query<HedgingRunDocument>().OrderByDescending(x => x.RunAt).Take(Math.Clamp(limit, 1, 500)).ToListAsync(ct));

    [HttpGet("sentiment/runs")]
    public async Task<ActionResult<List<SentimentRunDocument>>> ListSentimentRuns([FromQuery] int limit = 50, CancellationToken ct = default) =>
        Ok(await session.Query<SentimentRunDocument>().OrderByDescending(x => x.RunAt).Take(Math.Clamp(limit, 1, 500)).ToListAsync(ct));

    [HttpGet("model-validation/runs")]
    public async Task<ActionResult<List<ModelValidationRunDocument>>> ListModelValidationRuns([FromQuery] int limit = 50, CancellationToken ct = default) =>
        Ok(await session.Query<ModelValidationRunDocument>().OrderByDescending(x => x.RunAt).Take(Math.Clamp(limit, 1, 500)).ToListAsync(ct));

    [HttpGet("portfolio/runs")]
    public async Task<ActionResult<List<PortfolioRiskRunDocument>>> ListPortfolioRiskRuns([FromQuery] int limit = 50, CancellationToken ct = default) =>
        Ok(await session.Query<PortfolioRiskRunDocument>().OrderByDescending(x => x.RunAt).Take(Math.Clamp(limit, 1, 500)).ToListAsync(ct));
}
