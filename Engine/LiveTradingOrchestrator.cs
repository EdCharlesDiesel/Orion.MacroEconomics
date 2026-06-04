using Orion.MacroEconomics.DTO;
using Orion.MacroEconomics.Engine.Interfaces;
using Orion.MacroEconomics.Entities;
using Orion.MacroEconomics.Enum;
using Orion.MacroEconomics.Helpers;
using Orion.MacroEconomics.Models;

namespace Orion.MacroEconomics.Engine
{
    /// <summary>
    /// Drives the full live-trading pipeline:
    /// data quality → regime → scenario → probabilities → macro sim → signal → risk → sizing → execution → exit → trade plan.
    /// Returns a single <see cref="LiveTradingResult"/> describing the decision.
    /// </summary>
    public sealed class LiveTradingOrchestrator : ILiveTradingOrchestrator
    {
        private readonly IDataQualityEngine            _dataQuality;
        private readonly IRegimeEngine                 _regime;
        private readonly IScenarioEngine               _scenario;
        private readonly IProbabilisticScenarioEngine  _probabilisticScenario;
        private readonly IMacroSimulationEngine        _macroSimulation;
        private readonly ISignalEngine                 _signal;
        private readonly IRiskEngine                   _risk;
        private readonly IPositionSizingEngine         _positionSizing;
        private readonly IExecutionEngine              _execution;
        private readonly IExitEngine                   _exit;
        private readonly ITradeLifecycleEngine         _tradeLifecycle;
        private readonly IEconomicCalendarRiskEngine   _calendarRisk;

        public LiveTradingOrchestrator(
            IDataQualityEngine dataQuality,
            IRegimeEngine regime,
            IScenarioEngine scenario,
            IProbabilisticScenarioEngine probabilisticScenario,
            IMacroSimulationEngine macroSimulation,
            ISignalEngine signal,
            IRiskEngine risk,
            IPositionSizingEngine positionSizing,
            IExecutionEngine execution,
            IExitEngine exit,
            ITradeLifecycleEngine tradeLifecycle,
            IEconomicCalendarRiskEngine calendarRisk)
        {
            _dataQuality           = dataQuality;
            _regime                = regime;
            _scenario              = scenario;
            _probabilisticScenario = probabilisticScenario;
            _macroSimulation       = macroSimulation;
            _signal                = signal;
            _risk                  = risk;
            _positionSizing        = positionSizing;
            _execution             = execution;
            _exit                  = exit;
            _tradeLifecycle        = tradeLifecycle;
            _calendarRisk          = calendarRisk;
        }

        /// <inheritdoc />
        public LiveTradingResult Run(ForexMarketInput input, AccountContext account, OrderBook orderBook)
        {
            ArgumentNullException.ThrowIfNull(input);
            ArgumentNullException.ThrowIfNull(account);
            ArgumentNullException.ThrowIfNull(orderBook);

            // 1) Data quality gate
            var quality = _dataQuality.ValidateCandles(input.Candles);
            if (!quality.IsValid)
                return LiveTradingResult.Blocked("DATA_QUALITY", quality.Reason);

            // 2) Calendar risk gate
            var calendar = _calendarRisk.Evaluate(input, DateTime.UtcNow);
            if (calendar is { IsBlocked: true })
                return LiveTradingResult.Blocked("CALENDAR_RISK", calendar.Reason);

            // 3) Build a normalized indicator proxy from the latest candle set
            var market     = BuildMarketContext(input);
            var normalized = BuildNormalizedIndicator(input);

            // 4) Regime
            var regimeResult = _regime.Detect(normalized);

            // 5) Scenario + probabilities
            var scenarioResult     = _scenario.Build(normalized, regimeResult);
            var probabilityResult  = _probabilisticScenario.Calculate(normalized, regimeResult, scenarioResult);

            // 6) Macro simulation
            var macroSimResult = _macroSimulation.Simulate(normalized, regimeResult, probabilityResult);

            // 7) Signal
            var signal = _signal.Generate(market, regimeResult, scenarioResult, probabilityResult, macroSimResult);
            if (string.Equals(signal.Direction, "NO_TRADE", StringComparison.OrdinalIgnoreCase))
                return LiveTradingResult.Blocked("NO_SIGNAL", signal.Reason);

            // 8) Risk gate
            var risk = _risk.Evaluate(signal, market, regimeResult);
            if (!risk.IsAllowed)
                return LiveTradingResult.Blocked("RISK_BLOCKED", risk.Reason);

            // 9) Position sizing
            var size = _positionSizing.Calculate(signal, risk, market, account);
            if (!size.IsAllowed || size.PositionSize <= 0m)
                return LiveTradingResult.Blocked("SIZE_REJECTED", size.Reason);

            // 10) Execution against order book
            var execution = _execution.Execute(orderBook, signal.Direction, size.PositionSize);

            // 11) Exit plan
            var exit = _exit.Calculate(signal, execution, risk, normalized: null);

            // 12) Trade lifecycle
            var trade = _tradeLifecycle.CreatePlan(signal, risk, size, execution, exit);

            return new LiveTradingResult
            {
                Status       = "OK",
                Pair         = signal.Pair,
                Direction    = signal.Direction,
                Confidence   = signal.Confidence,
                Regime       = regimeResult.Name,
                Scenario     = scenarioResult.ScenarioName ?? scenarioResult.Name ?? string.Empty,
                PositionSize = size.PositionSize,
                EntryPrice   = execution.ExecutedPrice,
                StopLoss     = exit.StopLoss,
                TakeProfit   = exit.TakeProfit,
                RiskScore    = risk.Score,
                Reason       = signal.Reason,
                Trade        = trade
            };
        }

        private static NormalizedMarketContext BuildMarketContext(ForexMarketInput input)
        {
            return new NormalizedMarketContext
            {
                Pair    = input.Pair,
                Spread  = 0m,
                Candles = input.Candles ?? new List<OhlcvBar>()
            };
        }

        private static NormalizedIndicator BuildNormalizedIndicator(ForexMarketInput input)
        {
            var candles = input.Candles ?? new List<OhlcvBar>();
            var closes  = candles.Select(c => c.Close).ToList();

            decimal zScore       = 0m;
            decimal rollingMean  = 0m;
            decimal rollingStd   = 0m;
            decimal momPct       = 0m;
            decimal yoyPct       = 0m;

            if (closes.Count >= 2)
            {
                rollingMean = closes.Average();
                rollingStd  = DecimalMath.Sqrt(closes.Sum(c => (c - rollingMean) * (c - rollingMean)) / closes.Count);
                var last    = closes[^1];
                zScore      = rollingStd == 0m ? 0m : (last - rollingMean) / rollingStd;

                var prev = closes[^2];
                momPct   = prev == 0m ? 0m : (last - prev) / prev * 100m;

                var yoyIndex = Math.Max(0, closes.Count - 252);
                var yoyRef   = closes[yoyIndex];
                yoyPct       = yoyRef == 0m ? 0m : (last - yoyRef) / yoyRef * 100m;
            }

            return new NormalizedIndicator
            {
                Id            = Guid.NewGuid(),
                Country       = ExtractBaseCurrency(input.Pair),
                Indicator     = "FX_PRICE",
                Date          = input.Timestamp == default ? DateTime.UtcNow : input.Timestamp,
                Value         = closes.Count == 0 ? 0m : closes[^1],
                ZScore        = zScore,
                RollingMean   = rollingMean,
                RollingStdDev = rollingStd,
                MoM           = momPct,
                YoY           = yoyPct,
                Surprise      = 0m,
                Frequency     = "Daily",
                Name          = input.Pair
            };
        }

        private static string ExtractBaseCurrency(string pair)
        {
            if (string.IsNullOrWhiteSpace(pair)) return string.Empty;
            var clean = pair.Replace("/", "").Trim().ToUpperInvariant();
            return clean.Length >= 3 ? clean[..3] : clean;
        }
    }
}
