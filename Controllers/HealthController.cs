using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Orion.MacroEconomics.Engine.Interfaces;
using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Controllers;

[ApiController]
[Route("api/health")]
[Produces("application/json")]
public sealed class HealthController(
    IHealthCheckEngine healthCheckEngine,
    ILogger<HealthController> logger) : ControllerBase
{
    /// <summary>
    /// Runs all registered health checks and returns a full report.
    /// </summary>
    [HttpGet("run")]
    [ProducesResponseType(typeof(HealthReport), StatusCodes.Status200OK)]
    public async Task<IActionResult> RunHealthChecksAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Running system health checks");

        var report = await healthCheckEngine.RunHealthChecksAsync(cancellationToken);
        return Ok(report);
    }

    /// <summary>
    /// Returns the most recent cached health report.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(HealthReport), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCurrentHealthAsync()
    {
        var report = await healthCheckEngine.GetCurrentHealthAsync();
        return Ok(report);
    }

    /// <summary>
    /// Returns the health trend over a given number of hours (default 24).
    /// </summary>
    [HttpGet("trend")]
    [ProducesResponseType(typeof(HealthTrend), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetHealthTrendAsync([FromQuery] int hours = 24)
    {
        if (hours <= 0)
            return BadRequest("Hours must be greater than zero.");

        var trend = await healthCheckEngine.GetHealthTrendAsync(hours);
        return Ok(trend);
    }

    /// <summary>
    /// Returns detailed health information for a named component.
    /// </summary>
    [HttpGet("components/{componentName}")]
    [ProducesResponseType(typeof(ComponentDetails), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetComponentDetailsAsync([FromRoute] string componentName)
    {
        if (string.IsNullOrWhiteSpace(componentName))
            return BadRequest("Component name is required.");

        var details = await healthCheckEngine.GetComponentDetailsAsync(componentName);

        if (details is null)
            return NotFound($"Component '{componentName}' not found.");

        return Ok(details);
    }

    /// <summary>
    /// Registers a new health component.
    /// </summary>
    [HttpPost("components/register")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult RegisterComponent([FromBody] HealthComponent component)
    {
        if (component is null)
            return BadRequest("HealthComponent is required.");

        logger.LogInformation("Registering health component: {Name}", component.Name);

        healthCheckEngine.RegisterComponent(component);
        return NoContent();
    }

    /// <summary>
    /// Enables or disables a named health component.
    /// </summary>
    [HttpPatch("components/{componentName}/enabled")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult EnableComponent([FromRoute] string componentName, [FromQuery] bool enabled)
    {
        if (string.IsNullOrWhiteSpace(componentName))
            return BadRequest("Component name is required.");

        logger.LogInformation("{Action} health component: {Name}", enabled ? "Enabling" : "Disabling", componentName);

        healthCheckEngine.EnableComponent(componentName, enabled);
        return NoContent();
    }

    /// <summary>
    /// Initiates a graceful shutdown of all health-monitored components.
    /// </summary>
    [HttpPost("shutdown")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> GracefulShutdownAsync()
    {
        logger.LogWarning("Graceful shutdown initiated via API");

        await healthCheckEngine.GracefulShutdownAsync();
        return NoContent();
    }
}