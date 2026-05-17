using Orion.MacroEconomics.Models;

namespace Orion.MacroEconomics.Entities;

/// <summary>
/// Marten document wrapping a <see cref="MacroSnapshot"/> for one currency.
/// Id = ISO currency code, e.g. "USD".
/// </summary>
public sealed class MacroSnapshotDocument
{
    public string        Id          { get; set; } = string.Empty; // currency code
    public MacroSnapshot Snapshot    { get; set; } = new();
    public bool          IsLive      { get; set; }
    public DateTime      LastUpdated { get; set; }
}