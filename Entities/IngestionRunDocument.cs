namespace Orion.MacroEconomics.Entities;

/// <summary>
/// Persisted record of a single MarketIngestionJob execution.
/// Stored in Marten as a document — query via IQuerySession.
/// </summary>
public sealed class IngestionRunDocument
{
    public Guid            Id                     { get; set; }
    public DateTimeOffset  TriggeredAt            { get; set; }
    public DateTimeOffset  CompletedAt            { get; set; }
    public decimal         DurationSeconds        { get; set; }
    public int             TotalRecords           { get; set; }
    public bool            FullySuccessful        { get; set; }

    // ── Market sync ────────────────────────────────────────────────────────────
    public bool            MarketSyncSuccess      { get; set; }
    public string?         MarketSyncErrorMessage { get; set; }

    // ── Per-country macro results ──────────────────────────────────────────────
    public List<CountryIngestionResult> MacroResults { get; set; } = [];
}

public sealed class CountryIngestionResult
{
    public string  Country         { get; set; } = string.Empty;
    public bool    Success         { get; set; }
    public int     RecordsIngested { get; set; }
    public string? ErrorMessage    { get; set; }
}