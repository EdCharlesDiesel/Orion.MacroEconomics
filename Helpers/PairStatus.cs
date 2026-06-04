namespace Orion.MacroEconomics.Helpers;

public class PairStatus
{
    public string Pair { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
    public bool IsCached { get; set; }
    public TimeSpan? CacheAge { get; set; }

    public bool HasPair => !string.IsNullOrWhiteSpace(Pair);

    public bool IsFresh(TimeSpan maxAge) =>
        IsCached && CacheAge.HasValue && CacheAge.Value <= maxAge;

    public bool IsStale(TimeSpan maxAge) =>
        IsCached && CacheAge.HasValue && CacheAge.Value > maxAge;

    public override string ToString() =>
        $"{(HasPair ? Pair : "<unknown>")}: available={IsAvailable}, cached={IsCached}" +
        (CacheAge.HasValue ? $", age={CacheAge.Value.TotalSeconds:F0}s" : "");
}