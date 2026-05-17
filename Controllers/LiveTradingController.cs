using Microsoft.AspNetCore.Mvc;
using Orion.MacroEconomics.Engine.Interfaces;
using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Controllers
{
    [ApiController]
    [Route("api/live-trading")]
    public sealed class LiveTradingController : ControllerBase
    {
        private readonly ILiveTradingOrchestrator _orchestrator;

        public LiveTradingController(ILiveTradingOrchestrator orchestrator)
        {
            _orchestrator = orchestrator;
        }

        [HttpPost("run")]
        public ActionResult<LiveTradingResult> Run([FromBody] LiveTradingRequest request)
        {
            var result = _orchestrator.Run(
                request.MarketInput,
                request.Account,
                request.OrderBook);

            return Ok(result);

        
        }
    }
}
