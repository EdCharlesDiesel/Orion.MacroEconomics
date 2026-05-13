// using Orion.API.TradingEconomics.Engine.Interfaces;
//
// namespace Orion.API.TradingEconomics.Entities;
//
// /// <summary>
// /// Orchestrates full forex trading workflow from regime detection → execution.
// /// </summary>
// public sealed class LiveTradingOrchestrator : ILiveTradingOrchestrator
// {
//     private readonly IRegimeEngine              _regime;
//     private readonly IScenarioEngine            _scenario;
//     private readonly IProbabilisticScenarioEngine _probScenario;
//     private readonly IMacroSimulationEngine     _macro;
//     private readonly ISignalEngine              _signal;
//     private readonly IRiskEngine                _risk;
//     private readonly IPositionSizingEngine      _positionSizing;
//     private readonly IFxPricingEngine           _pricing;
//     private readonly IExecutionEngine           _execution;
//     private readonly IExitEngine                _exit;
//     private readonly ITradeLifecycleEngine      _tradeLifecycle;
//
//     public LiveTradingOrchestrator(
//         IRegimeEngine               regime,
//         IScenarioEngine             scenario,
//         IProbabilisticScenarioEngine probabilisticScenario,
//         IMacroSimulationEngine      macroSimulation,
//         ISignalEngine               signal,
//         IRiskEngine                 risk,
//         IPositionSizingEngine       positionSizing,
//         IFxPricingEngine            pricing,
//         IExecutionEngine            execution,
//         IExitEngine                 exit,
//         ITradeLifecycleEngine       tradeLifecycle)
//     {
//         _regime         = regime                ?? throw new ArgumentNullException(nameof(regime));
//         _scenario       = scenario              ?? throw new ArgumentNullException(nameof(scenario));
//         _probScenario   = probabilisticScenario ?? throw new ArgumentNullException(nameof(probabilisticScenario));
//         _macro          = macroSimulation       ?? throw new ArgumentNullException(nameof(macroSimulation));
//         _signal         = signal                ?? throw new ArgumentNullException(nameof(signal));
//         _risk           = risk                  ?? throw new ArgumentNullException(nameof(risk));
//         _positionSizing = positionSizing        ?? throw new ArgumentNullException(nameof(positionSizing));
//         _pricing        = pricing               ?? throw new ArgumentNullException(nameof(pricing));
//         _execution      = execution             ?? throw new ArgumentNullException(nameof(execution));
//         _exit           = exit                  ?? throw new ArgumentNullException(nameof(exit));
//         _tradeLifecycle = tradeLifecycle        ?? throw new ArgumentNullException(nameof(tradeLifecycle));
//     }
//
//     /// <inheritdoc />
//     public LiveTradingResult Run(ForexMarketInput input, AccountContext account, OrderBook orderBook)
//     {
//         ArgumentNullException.ThrowIfNull(input);
//         ArgumentNullException.ThrowIfNull(account);
//         ArgumentNullException.ThrowIfNull(orderBook);
//
//         var marketContext_ = new NormalizedMarketContext
//         {
//             Pair    = input.Pair,
//             Candles = input.Candles
//         };
//         
//         var marketContext = new NormalizedIndicator()
//         {
//             Pair    = input.Pair,
//             Candles = input.Candles
//         };
//
//         var regime        = _regime.Detect(marketContext);
//         var scenario      = _scenario.Build(marketContext, regime);
//         var probabilities = _probScenario.Calculate(marketContext, regime, scenario);
//         var macro         = _macro.Simulate(marketContext, regime, probabilities);
//
//         var signal = _signal.Generate(marketContext, regime, scenario, probabilities, macro);
//         if (signal.Direction == "NO_TRADE")
//             return LiveTradingResult.Blocked("SIGNAL_BLOCKED", signal.Reason);
//
//         var risk = _risk.Evaluate(signal, marketContext, regime);
//         if (!risk.IsAllowed)
//             return LiveTradingResult.Blocked("RISK_BLOCKED", risk.Reason);
//
//         var size = _positionSizing.Calculate(signal, risk, marketContext, account);
//         if (!size.IsAllowed)
//             return LiveTradingResult.Blocked("POSITION_SIZE_BLOCKED", size.Reason);
//
//         _pricing.Price(signal.Pair, signal.Direction, size.PositionSize);
//
//         var execution = _execution.Execute(orderBook, signal.Direction, size.PositionSize);
//         if (execution.FilledSize <= 0)
//             return LiveTradingResult.Blocked("EXECUTION_FAILED", "Order was not filled.");
//
//         var exit  = _exit.Calculate(signal, execution, risk, marketContext);
//         var trade = _tradeLifecycle.CreatePlan(signal, risk, size, execution, exit);
//
//         return new LiveTradingResult
//         {
//             Status       = trade.Status,
//             Pair         = trade.Pair,
//             Direction    = trade.Direction,
//             Confidence   = signal.Confidence,
//             Regime       = regime.Regime.ToString(),
//             Scenario     = scenario.ScenarioName,
//             PositionSize = trade.PositionSize,
//             EntryPrice   = trade.EntryPrice,
//             StopLoss     = trade.StopLoss,
//             TakeProfit   = trade.TakeProfit,
//             RiskScore    = risk.Score,
//             Reason       = trade.Reason,
//             Trade        = trade
//         };
//     }
// }