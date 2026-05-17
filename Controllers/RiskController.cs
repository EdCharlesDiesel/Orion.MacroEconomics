using Microsoft.AspNetCore.Mvc;
using Orion.MacroEconomics.Engine.Interfaces;
using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Controllers
{
    [ApiController]
    [Route("api/risk")]
    [Produces("application/json")]
    public sealed class RiskController(ICircuitBreakerEngine circuitBreakerEngine,IComplianceEngine complianceEngine,IEconomicCalendarRiskEngine economicCalendarRiskEngine,ILogger<RiskController> logger) : ControllerBase
    {
        /// <summary>
        /// Evaluates circuit breaker conditions based on account state and trade history.
        /// </summary>
        [HttpPost("circuit-breaker/evaluate")]
        [ProducesResponseType(typeof(CircuitBreakerResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult EvaluateCircuitBreaker([FromBody] CircuitBreakerRequest request)
        {
            if (request.Account is null)
                return BadRequest("Account context is required.");

            logger.LogInformation("Evaluating circuit breaker for account");

            var result = circuitBreakerEngine.Evaluate(
                request.Account,
                request.TodayTrades,
                request.OpenTrades,
                request.DataQuality);

            return Ok(result);
        }

        /// <summary>
        /// Validates a proposed trade against compliance rules.
        /// </summary>
        [HttpPost("compliance/validate")]
        [ProducesResponseType(typeof(ComplianceResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult ValidateCompliance([FromBody] ComplianceRequest request)
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

            return Ok(result);
        }
        
        /// <summary>
        /// Evaluates economic calendar risk for a forex pair at the current time.
        /// </summary>
        [HttpPost("economic-calendar/evaluate")]
        [ProducesResponseType(typeof(EconomicCalendarRiskResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public IActionResult EvaluateEconomicCalendarRisk([FromBody] EconomicCalendarRiskRequest request)
        {
            if (request.Input is null)
                return BadRequest("ForexMarketInput is required.");
 
            var nowUtc = request.NowUtc == default ? DateTime.UtcNow : request.NowUtc;
 
            logger.LogInformation("Evaluating economic calendar risk for {Pair} at {Time}", request.Input.Pair, nowUtc);
 
            var result = economicCalendarRiskEngine.Evaluate(request.Input, nowUtc);
            return Ok(result);
        }
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
