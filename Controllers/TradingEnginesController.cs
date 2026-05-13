using Microsoft.AspNetCore.Mvc;
using Orion.MacroEconomics.DTO;
using Orion.MacroEconomics.Engine.Interfaces;
using Orion.MacroEconomics.Engine.Interfaces.Orion.API.TradingEconomics.Engine.Interfaces;
using Orion.MacroEconomics.Entities;
using Orion.MacroEconomics.Enum;
using RegimeResult = Orion.MacroEconomics.Entities.RegimeResult;

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
    IMarketDataEngine marketData)
    : ControllerBase
{
    [HttpPost("data-quality/validate")]
    public ActionResult<DataQualityResult> ValidateDataQuality(ForexMarketInput input)
        => Ok(dataQuality.ValidateCandles(input.Candles));

    [HttpPost("normalization/normalize")]
    public ActionResult<List<NormalizedIndicator>> Normalize(List<EconomicIndicator> indicators)
        => Ok(normalization.Normalize(indicators));

    [HttpPost("regime/detect")]
    public ActionResult<RegimeResult> DetectRegime(NormalizedIndicator indicator)
        => Ok(regime.Detect(indicator));

    [HttpGet("regime/next")]
    public ActionResult<MarketRegime> NextRegime([FromQuery] MarketRegime current)
        => Ok(regime.Next(current));

    [HttpPost("scenario/run")]
    public async Task<ActionResult<ScenarioResult>> RunScenario(
        Scenario request,
        CancellationToken cancellationToken)
        => Ok(await scenario.RunAsync(request, cancellationToken));

    [HttpPost("scenario/build")]
    public ActionResult<ScenarioResult> BuildScenario(ScenarioBuildRequest request)
        => Ok(scenario.Build(request.Normalized, request.Regime));

    [HttpPost("probabilistic-scenario/calculate")]
    public ActionResult<ProbabilisticScenarioResult> CalculateProbabilisticScenario(
        ProbabilisticScenarioRequest request)
        => Ok(probabilisticScenario.Calculate(
            request.Normalized,
            request.Regime,
            request.Scenario));

    [HttpPost("macro-simulation/simulate")]
    public ActionResult<MacroSimulationResult> SimulateMacro(MacroSimulationRequest request)
        => Ok(macroSimulation.Simulate(
            request.Normalized,
            request.Regime,
            request.Probabilities));

    [HttpPost("signal/generate")]
    public ActionResult<SignalResult> GenerateSignal(SignalRequest request)
        => Ok(signal.Generate(
            request.Market,
            request.Regime,
            request.Scenario,
            request.Probabilities,
            request.MacroSimulation));

    [HttpPost("risk/evaluate")]
    public ActionResult<RiskResult> EvaluateRisk(RiskRequest request)
        => Ok(risk.Evaluate(
            request.Signal,
            request.Market,
            request.Regime));

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
        => Ok(await correlation.AnalyzeAsync(request, cancellationToken));

    [HttpPost("liquidity/analyze")]
    public async Task<ActionResult<LiquidityResult>> AnalyzeLiquidity(
        LiquidityRequest request,
        CancellationToken cancellationToken)
        => Ok(await liquidity.AnalyzeAsync(request, cancellationToken));

    [HttpPost("hedging/analyze")]
    public async Task<ActionResult<HedgingResult>> AnalyzeHedging(
        HedgingRequest request,
        CancellationToken cancellationToken)
        => Ok(await hedging.AnalyzeAsync(request, cancellationToken));

    [HttpPost("sentiment/analyze")]
    public async Task<ActionResult<SentimentResult>> AnalyzeSentiment(
        SentimentRequest request,
        CancellationToken cancellationToken)
        => Ok(await sentiment.AnalyzeAsync(request, cancellationToken));

    [HttpPost("performance/analyze")]
    public ActionResult<PerformanceReport> AnalyzePerformance(List<TradePlan> trades)
        => Ok(performance.Analyze(trades));

    [HttpPost("model-validation/validate")]
    public ActionResult<ModelValidationReport> ValidateModel(ModelValidationRequest request)
        => Ok(modelValidation.Validate(
            request.Performance,
            request.Trades));

    [HttpPost("portfolio/evaluate")]
    public ActionResult<PortfolioRiskResult> EvaluatePortfolio(PortfolioRiskRequest request)
        => Ok(portfolio.Evaluate(
            request.NewTrade,
            request.OpenTrades,
            request.Account));

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

    [HttpGet("market-data/fred-mappings")]
    public ActionResult<Dictionary<string, Dictionary<string, string>>> GetFredMappings()
        => Ok(marketData.GetFredSeriesMappings());

    [HttpGet("market-data/status")]
    public async Task<ActionResult<FredStatusResponse>> CheckFredStatus(CancellationToken cancellationToken)
        => Ok(await marketData.CheckStatusAsync(cancellationToken));

    [HttpGet("market-data/health")]
    public async Task<ActionResult<MarketDataHealth>> CheckMarketDataHealth(
        [FromQuery] string pair,
        CancellationToken cancellationToken)
        => Ok(await marketData.CheckHealthAsync(pair, cancellationToken));
}