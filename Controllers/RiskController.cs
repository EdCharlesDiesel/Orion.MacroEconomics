using Marten;
using Microsoft.AspNetCore.Mvc;
using Orion.MacroEconomics.Engine.Interfaces;
using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Controllers
{
    [ApiController]
    [Route("api/risk")]
    [Produces("application/json")]
    public sealed class RiskController(
        ICircuitBreakerEngine circuitBreakerEngine,
        IComplianceEngine complianceEngine,
        IEconomicCalendarRiskEngine economicCalendarRiskEngine,
        IDocumentSession session,
        ILogger<RiskController> logger) : ControllerBase
    {
        /// <summary>
        /// Evaluates circuit breaker conditions based on account state and trade history.
        /// Persists every evaluation to Postgres.
        /// </summary>
        [HttpPost("circuit-breaker/evaluate")]
        [ProducesResponseType(typeof(CircuitBreakerResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> EvaluateCircuitBreaker([FromBody] CircuitBreakerRequest request, CancellationToken ct)
        {
            if (request.Account is null)
                return BadRequest("Account context is required.");

            logger.LogInformation("Evaluating circuit breaker for account");

            var result = circuitBreakerEngine.Evaluate(
                request.Account,
                request.TodayTrades,
                request.OpenTrades,
                request.DataQuality);

            var doc = new CircuitBreakerRunDocument
            {
                TodayTradeCount = request.TodayTrades?.Count ?? 0,
                OpenTradeCount  = request.OpenTrades?.Count  ?? 0,
                Result          = result ?? new CircuitBreakerResult()
            };
            session.Store(doc);
            await session.SaveChangesAsync(ct);

            return Ok(result);
        }

        /// <summary>
        /// Validates a proposed trade against compliance rules.
        /// Persists every validation to Postgres.
        /// </summary>
        [HttpPost("compliance/validate")]
        [ProducesResponseType(typeof(ComplianceResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ValidateCompliance([FromBody] ComplianceRequest request, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(request.Pair))
                return BadRequest("Pair is required.");

            if (string.IsNullOrWhiteSpace(request.Direction))
                return BadRequest("Direction is required.");

            if (request.RequestedSize <= 0)
                return BadRequest("RequestedSize must be greater than zero.");

            if (request.Account is null)
                return BadRequest("Account snapshot is required.");

            if (request.Risk is null)
                return BadRequest("Risk result is required.");

            logger.LogInformation("Validating compliance for {Pair} {Direction}", request.Pair, request.Direction);

            var result = complianceEngine.Validate(
                request.Pair,
                request.Direction,
                request.RequestedSize,
                request.Account,
                request.Risk);

            var doc = new ComplianceRunDocument
            {
                Pair          = request.Pair,
                Direction     = request.Direction,
                RequestedSize = request.RequestedSize,
                Result        = result ?? new ComplianceResult()
            };
            session.Store(doc);
            await session.SaveChangesAsync(ct);

            return Ok(result);
        }

        /// <summary>
        /// Evaluates economic calendar risk for a forex pair at the current time.
        /// Persists every evaluation to Postgres.
        /// </summary>
        [HttpPost("economic-calendar/evaluate")]
        [ProducesResponseType(typeof(EconomicCalendarRiskResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> EvaluateEconomicCalendarRisk([FromBody] EconomicCalendarRiskRequest request, CancellationToken ct)
        {
            if (request.Input is null)
                return BadRequest("ForexMarketInput is required.");

            var nowUtc = request.NowUtc == default ? DateTime.UtcNow : request.NowUtc;

            logger.LogInformation("Evaluating economic calendar risk for {Pair} at {Time}", request.Input.Pair, nowUtc);

            var result = economicCalendarRiskEngine.Evaluate(request.Input, nowUtc);

            var doc = new EconomicCalendarRiskRunDocument
            {
                Pair        = request.Input.Pair ?? string.Empty,
                EvaluatedAt = nowUtc,
                Result      = result ?? new EconomicCalendarRiskResult()
            };
            session.Store(doc);
            await session.SaveChangesAsync(ct);

            return Ok(result);
        }

        /// <summary>Lists the most recent persisted circuit-breaker evaluations.</summary>
        [HttpGet("circuit-breaker/runs")]
        [ProducesResponseType(typeof(List<CircuitBreakerRunDocument>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ListCircuitBreakerRuns([FromQuery] int limit = 50, CancellationToken ct = default) =>
            Ok(await session.Query<CircuitBreakerRunDocument>()
                .OrderByDescending(x => x.RunAt)
                .Take(Math.Clamp(limit, 1, 500))
                .ToListAsync(ct));

        /// <summary>Lists the most recent persisted compliance validations.</summary>
        [HttpGet("compliance/runs")]
        [ProducesResponseType(typeof(List<ComplianceRunDocument>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ListComplianceRuns([FromQuery] int limit = 50, CancellationToken ct = default) =>
            Ok(await session.Query<ComplianceRunDocument>()
                .OrderByDescending(x => x.RunAt)
                .Take(Math.Clamp(limit, 1, 500))
                .ToListAsync(ct));

        /// <summary>Lists the most recent persisted calendar-risk evaluations.</summary>
        [HttpGet("economic-calendar/runs")]
        [ProducesResponseType(typeof(List<EconomicCalendarRiskRunDocument>), StatusCodes.Status200OK)]
        public async Task<IActionResult> ListEconomicCalendarRuns([FromQuery] int limit = 50, CancellationToken ct = default) =>
            Ok(await session.Query<EconomicCalendarRiskRunDocument>()
                .OrderByDescending(x => x.RunAt)
                .Take(Math.Clamp(limit, 1, 500))
                .ToListAsync(ct));
    }

    public sealed record CircuitBreakerRequest(
        AccountContext Account,
        List<TradePlan>? TodayTrades,
        List<TradePlan>? OpenTrades,
        DataQualityResult? DataQuality);

    public sealed record ComplianceRequest(
        string Pair,
        string Direction,
        decimal RequestedSize,
        AccountSnapshot Account,
        RealTimeRiskResult Risk);

    public sealed record EconomicCalendarRiskRequest(ForexMarketInput Input, DateTime NowUtc);
}
