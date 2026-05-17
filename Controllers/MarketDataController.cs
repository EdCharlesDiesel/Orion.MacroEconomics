using Microsoft.AspNetCore.Mvc;
using Orion.MacroEconomics.Engine.Interfaces;

namespace Orion.MacroEconomics.Controllers;

[ApiController]
[Route("api/market-data")]
[Produces("application/json")]
public sealed class MarketDataController(
    IMarketDataEngine engine,
    ILogger<MarketDataController> logger) : ControllerBase
{
    // ── Macro ──────────────────────────────────────────────────────────────────

    /// <summary>Returns the latest macro data snapshot.</summary>
    [HttpGet("macro")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMacroDataAsync(CancellationToken cancellationToken)
    {
        var result = await engine.GetMacroDataAsync(cancellationToken);
        return result is null ? NotFound("No macro data available.") : Ok(result);
    }

    /// <summary>Forces a refresh of macro data and returns the updated snapshot.</summary>
    [HttpPost("macro/refresh")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RefreshMacroDataAsync(CancellationToken cancellationToken)
    {
        var result = await engine.RefreshMacroDataAsync(cancellationToken);
        return result is null ? NotFound("Refresh returned no data.") : Ok(result);
    }

    // ── Ticks ──────────────────────────────────────────────────────────────────

    /// <summary>Returns the latest tick for a currency pair.</summary>
    [HttpGet("tick/{pair}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLatestTickAsync(
        [FromRoute] string pair,
        CancellationToken cancellationToken)
    {
        var result = await engine.GetLatestTickAsync(pair, cancellationToken);
        return result is null ? NotFound($"No tick found for pair '{pair}'.") : Ok(result);
    }

    // ── Health ─────────────────────────────────────────────────────────────────

    /// <summary>Checks the health of all market data providers for a given pair.</summary>
    [HttpGet("health/{pair}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CheckHealthAsync(
        [FromRoute] string pair,
        CancellationToken cancellationToken)
    {
        var result = await engine.CheckHealthAsync(pair, cancellationToken);
        return result is null ? NotFound($"No health data for pair '{pair}'.") : Ok(result);
    }

    // ── Snapshots ──────────────────────────────────────────────────────────────

    /// <summary>Returns a stored market data snapshot by id.</summary>
    [HttpGet("snapshots/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByIdAsync(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var result = await engine.GetByIdAsync(id, cancellationToken);
        return result is null ? NotFound($"Snapshot '{id}' not found.") : Ok(result);
    }

    /// <summary>Returns all snapshots for a symbol, with optional date range filter.</summary>
    [HttpGet("snapshots/symbol/{symbol}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBySymbolAsync(
        [FromRoute] string symbol,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        CancellationToken cancellationToken)
    {
        var result = await engine.GetBySymbolAsync(symbol, fromUtc, toUtc, cancellationToken);
        return Ok(result);
    }

    /// <summary>Returns all snapshots for a provider, optionally filtered by data type.</summary>
    [HttpGet("snapshots/provider/{providerName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByProviderAsync(
        [FromRoute] string providerName,
        [FromQuery] string? dataType,
        CancellationToken cancellationToken)
    {
        var result = await engine.GetByProviderAsync(providerName, dataType, cancellationToken);
        return Ok(result);
    }

    /// <summary>Checks whether a snapshot exists for the given symbol and date range.</summary>
    [HttpGet("snapshots/exists/{symbol}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ExistsAsync(
        [FromRoute] string symbol,
        [FromQuery] DateTime fromUtc,
        [FromQuery] DateTime toUtc,
        CancellationToken cancellationToken)
    {
        var exists = await engine.ExistsAsync(symbol, fromUtc, toUtc, cancellationToken);
        return Ok(new { symbol, fromUtc, toUtc, exists });
    }

    /// <summary>Deletes a snapshot by id.</summary>
    [HttpDelete("snapshots/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteAsync(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var deleted = await engine.DeleteAsync(id, cancellationToken);
        return deleted ? Ok(new { id, deleted = true }) : NotFound($"Snapshot '{id}' not found.");
    }

    // ── Fetch & Store ──────────────────────────────────────────────────────────

    /// <summary>
    /// Fetches market data from the specified provider for a symbol and date range,
    /// persists it, and returns the stored snapshot.
    /// </summary>
    [HttpPost("fetch-and-store")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> FetchAndStoreAsync(
        [FromBody] FetchAndStoreRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await engine.FetchAndStoreAsync(
            request.Provider,
            request.Symbol,
            request.FromUtc,
            request.ToUtc,
            cancellationToken);

        return Ok(result);
    }

    // ── Save ───────────────────────────────────────────────────────────────────

    /// <summary>Directly persists a market data payload as a snapshot.</summary>
    [HttpPost("snapshots")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SaveAsync(
        [FromBody] SaveSnapshotRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var snapshot = await engine.SaveAsync(
            request.ProviderName,
            request.DataType,
            request.Symbol,
            request.FromUtc,
            request.ToUtc,
            request.Payload,
            cancellationToken);

        return CreatedAtAction(
            nameof(GetByIdAsync),
            new { id = snapshot.Id },
            snapshot);
    }
}

// ── Request DTOs ───────────────────────────────────────────────────────────────

public sealed record FetchAndStoreRequest(
    string Provider,
    string Symbol,
    DateTime FromUtc,
    DateTime ToUtc);

public sealed record SaveSnapshotRequest(
    string ProviderName,
    string DataType,
    string Symbol,
    DateTime FromUtc,
    DateTime ToUtc,
    object Payload);