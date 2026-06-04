using Microsoft.AspNetCore.Mvc;
using Orion.MacroEconomics.Engine.Interfaces;
using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Controllers;

[ApiController]
[Route("api/configuration")]
[Produces("application/json")]
public sealed class ConfigurationController(IConfigurationEngine configurationEngine) : ControllerBase
{
    /// <summary>
    /// Returns the full trading system configuration.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(TradingSystemConfig), StatusCodes.Status200OK)]
    public IActionResult GetConfig()
    {
        return Ok(configurationEngine.GetConfig());
    }

    /// <summary>
    /// Returns configuration for a specific forex pair.
    /// </summary>
    [HttpGet("pairs/{pair}")]
    [ProducesResponseType(typeof(PairConfig), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult GetPairConfig([FromRoute] string pair)
    {
        if (string.IsNullOrWhiteSpace(pair))
            return BadRequest("Pair is required.");

        return Ok(configurationEngine.GetPairConfig(pair));
    }

    /// <summary>
    /// Returns whether live trading is currently enabled.
    /// </summary>
    [HttpGet("live-trading/enabled")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public IActionResult IsLiveTradingEnabled()
    {
        return Ok(configurationEngine.IsLiveTradingEnabled());
    }

    /// <summary>
    /// Returns whether a specific pair is enabled for trading.
    /// </summary>
    [HttpGet("pairs/{pair}/enabled")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult IsPairEnabled([FromRoute] string pair)
    {
        if (string.IsNullOrWhiteSpace(pair))
            return BadRequest("Pair is required.");

        return Ok(configurationEngine.IsPairEnabled(pair));
    }

    /// <summary>
    /// Returns the current risk configuration.
    /// </summary>
    [HttpGet("risk")]
    [ProducesResponseType(typeof(RiskConfig), StatusCodes.Status200OK)]
    public IActionResult GetRiskConfig()
    {
        return Ok(configurationEngine.GetRiskConfig());
    }

    /// <summary>
    /// Returns the current signal configuration.
    /// </summary>
    [HttpGet("signals")]
    [ProducesResponseType(typeof(SignalConfig), StatusCodes.Status200OK)]
    public IActionResult GetSignalConfig()
    {
        return Ok(configurationEngine.GetSignalConfig());
    }

    /// <summary>
    /// Returns the current execution configuration.
    /// </summary>
    [HttpGet("execution")]
    [ProducesResponseType(typeof(ExecutionConfig), StatusCodes.Status200OK)]
    public IActionResult GetExecutionConfig()
    {
        return Ok(configurationEngine.GetExecutionConfig());
    }
}