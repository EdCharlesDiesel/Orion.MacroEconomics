using MediatR;
using Microsoft.AspNetCore.Mvc;
using Orion.MacroEconomics.Commands;
using Orion.MacroEconomics.DTO;
using Orion.MacroEconomics.Engine.Interfaces;
using Orion.MacroEconomics.Entities;
using Orion.MacroEconomics.Helpers;
using Orion.MacroEconomics.Interfaces;

namespace Orion.MacroEconomics.Controllers
{
    [ApiController]
    [Route("api/macro")]
    public sealed class MacroController(
        IMediator mediator,
        IFredService fredService,
        ILogger<MacroController> logger,
        IScenarioEngine scenarioEngine,
        IProbabilisticScenarioGenerator generator,
        IProbabilisticScenarioEngine engine) : ControllerBase
    {
        [HttpPost("ingest/{country}")]
        public async Task<IActionResult> Ingest(string country)
        {
            await mediator.Send(new IngestMacroDataCommand(country));
            return Ok();
        }

        [HttpPost("normalize")]
        public async Task<IActionResult> Normalize([FromQuery] bool forceRefresh = false)
        {
            var count = await mediator.Send(new NormalizeMacroDataCommand(forceRefresh));

            return Ok(new
            {
                normalized = count,
                forceRefresh
            });
        }

        [HttpGet("factors")]
        public async Task<IActionResult> GetFactors()
        {
            var result = await mediator.Send(new CalculateCurrencyFactorsCommand());
            return Ok(result);
        }

        [HttpGet("signals")]
        public async Task<IActionResult> GetSignals()
        {
            var result = await mediator.Send(new GenerateFxSignalsCommand());
            return Ok(result);
        }

        [HttpGet("portfolio")]
        public async Task<IActionResult> GetPortfolio([FromQuery] decimal capital = 100000)
        {
            var result = await mediator.Send(new BuildPortfolioCommand(capital));
            return Ok(result);
        }

        [HttpGet]
        public async Task<ActionResult<MacroData>> GetMacroData(
            CancellationToken cancellationToken = default)
        {
            try
            {
                var macroData = await fredService.GetMacroDataAsync(cancellationToken);
                return Ok(macroData);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error fetching macro data from FRED");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("{currency}")]
        public async Task<ActionResult<CurrencyMacroData>> GetMacroDataForCurrency(
            string currency,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var macroData = await fredService.GetMacroDataAsync(cancellationToken);

                if (macroData.Data.TryGetValue(currency.ToUpperInvariant(), out var currencyData))
                    return Ok(currencyData);

                return NotFound(new { error = $"Currency '{currency}' not found" });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error fetching macro data for {Currency}", currency);
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("series")]
        public ActionResult<Dictionary<string, Dictionary<string, string>>> GetFredSeries()
        {
            try
            {
                var series = fredService.GetFredSeriesMappings();
                return Ok(series);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error fetching FRED series mappings");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("status")]
        public async Task<ActionResult<FredStatusResponse>> GetStatus(
            CancellationToken cancellationToken = default)
        {
            try
            {
                var status = await fredService.CheckStatusAsync(cancellationToken);
                return Ok(status);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error checking FRED status");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("refresh")]
        public async Task<ActionResult<MacroData>> RefreshMacroData(
            CancellationToken cancellationToken = default)
        {
            try
            {
                var macroData = await fredService.RefreshMacroDataAsync(cancellationToken);
                return Ok(macroData);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error refreshing macro data");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpPost("scenario/run")]
        public async Task<IActionResult> RunScenario([FromBody] Scenario scenario)
        {
            var result = await scenarioEngine.RunAsync(scenario);
            return Ok(result);
        }

        [HttpGet("scenario/probabilistic")]
        public async Task<IActionResult> RunProbabilistic([FromQuery] int simulations = 500)
        {
            var scenarios = generator.Generate(simulations);
            var results = await engine.RunAsync(scenarios);
            var aggregated = ProbabilisticAggregator.Aggregate(results);

            return Ok(aggregated);
        }
    }
}