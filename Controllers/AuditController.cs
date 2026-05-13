using Microsoft.AspNetCore.Mvc;
using Orion.MacroEconomics.Engine.Interfaces;
using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Controllers;

[ApiController]
[Route("api/audit")]
[Produces("application/json")]
public sealed class AuditController(
    IAuditTrailEngine auditTrailEngine,
    ILogger<AuditController> logger) : ControllerBase
{
    /// <summary>
    /// Records a full trading decision audit record.
    /// </summary>
    [HttpPost("decisions")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RecordDecisionAsync([FromBody] AuditRecord record)
    {
        if (record is null)
            return BadRequest("Audit record is required.");

        logger.LogInformation("Recording audit decision");

        var id = await auditTrailEngine.RecordDecisionAsync(record);
        return CreatedAtAction(nameof(QueryAsync), new { correlationId = id }, id);
    }

    /// <summary>
    /// Records a single pipeline step for an existing correlation ID.
    /// </summary>
    [HttpPost("steps/{correlationId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RecordPipelineStepAsync(
        [FromRoute] Guid correlationId,
        [FromBody] PipelineStepRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.StepName))
            return BadRequest("StepName is required.");

        if (request.StepData is null)
            return BadRequest("StepData is required.");

        await auditTrailEngine.RecordPipelineStepAsync(correlationId, request.StepName, request.StepData, request.Duration);
        return NoContent();
    }

    /// <summary>
    /// Records an execution error against a correlation ID.
    /// </summary>
    [HttpPost("errors/{correlationId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RecordErrorAsync(
        [FromRoute] Guid correlationId,
        [FromBody] AuditErrorRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Stage))
            return BadRequest("Stage is required.");

        if (string.IsNullOrWhiteSpace(request.Message))
            return BadRequest("Exception message is required.");

        var exception = new Exception(request.Message);
        await auditTrailEngine.RecordErrorAsync(correlationId, request.Stage, exception, request.Context);
        return NoContent();
    }

    /// <summary>
    /// Records a business or system event.
    /// </summary>
    [HttpPost("events/{correlationId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RecordEventAsync(
        [FromRoute] Guid correlationId,
        [FromBody] AuditEventRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.EventName))
            return BadRequest("EventName is required.");

        await auditTrailEngine.RecordEventAsync(correlationId, request.EventName, request.Metadata);
        return NoContent();
    }

    /// <summary>
    /// Flushes all buffered audit records to storage.
    /// </summary>
    [HttpPost("flush")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> FlushAsync()
    {
        logger.LogInformation("Flushing audit trail buffer");
        await auditTrailEngine.FlushAsync();
        return NoContent();
    }

    /// <summary>
    /// Queries stored audit records.
    /// </summary>
    [HttpPost("query")]
    [ProducesResponseType(typeof(AuditQueryResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> QueryAsync([FromBody] AuditQuery query)
    {
        if (query is null)
            return BadRequest("Query is required.");

        var result = await auditTrailEngine.QueryAsync(query);
        return Ok(result);
    }

    /// <summary>
    /// Generates a compliance report for a given date range and optional pair.
    /// </summary>
    [HttpGet("compliance/report")]
    [ProducesResponseType(typeof(ComplianceReport), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GenerateComplianceReportAsync(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] string? pair = null)
    {
        if (startDate == default || endDate == default)
            return BadRequest("startDate and endDate are required.");

        if (startDate >= endDate)
            return BadRequest("startDate must be before endDate.");

        logger.LogInformation("Generating compliance report from {Start} to {End}", startDate, endDate);

        var report = await auditTrailEngine.GenerateComplianceReportAsync(startDate, endDate, pair);
        return Ok(report);
    }
}

public sealed record PipelineStepRequest(string StepName, object StepData, TimeSpan Duration);
public sealed record AuditErrorRequest(string Stage, string Message, Dictionary<string, object>? Context);
public sealed record AuditEventRequest(string EventName, Dictionary<string, object>? Metadata);