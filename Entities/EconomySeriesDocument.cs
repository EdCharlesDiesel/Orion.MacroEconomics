namespace Orion.MacroEconomics.Entities;

/// <summary>
/// Marten document that stores all data points for a single economic series.
/// Id = series identifier, e.g. "FEDFUNDS", "CPIAUCSL".
/// DataPoints are kept sorted by date ascending; duplicates are deduplicated on upsert.
/// </summary>
public sealed class EconomySeriesDocument
{
    public string                 Id          { get; set; } = string.Empty;
    public List<EconomyDataPoint> DataPoints  { get; set; } = [];
    public DateTime               LastUpdated { get; set; }
}