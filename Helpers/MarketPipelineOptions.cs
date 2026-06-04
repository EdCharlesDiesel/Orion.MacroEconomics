namespace Orion.MacroEconomics.Helpers;

public class MarketPipelineOptions
{
    public bool EnableCaching { get; set; } = true;
    public int CacheExpirationSeconds { get; set; } = 300; // 5 minutes
    public int CacheSlidingExpirationSeconds { get; set; } = 60;
    public int ValidationRetries { get; set; } = 2;
    public bool EnableEnrichment { get; set; } = true;
    public bool EnrichWithCorrelations { get; set; } = true;
    public bool EnrichWithSentiment { get; set; } = false;
    public bool EnrichWithMacroCalendar { get; set; } = true;
    public int QuickSnapshotCacheSeconds { get; set; } = 30;
    public int MaxConcurrentPairs { get; set; } = 5;

    public TimeSpan CacheExpiration         => TimeSpan.FromSeconds(CacheExpirationSeconds);
    public TimeSpan CacheSlidingExpiration  => TimeSpan.FromSeconds(CacheSlidingExpirationSeconds);
    public TimeSpan QuickSnapshotCache      => TimeSpan.FromSeconds(QuickSnapshotCacheSeconds);

    public bool AnyEnrichmentEnabled =>
        EnableEnrichment &&
        (EnrichWithCorrelations || EnrichWithSentiment || EnrichWithMacroCalendar);

    public void Validate()
    {
        if (CacheExpirationSeconds        < 0) throw new ArgumentOutOfRangeException(nameof(CacheExpirationSeconds));
        if (CacheSlidingExpirationSeconds < 0) throw new ArgumentOutOfRangeException(nameof(CacheSlidingExpirationSeconds));
        if (QuickSnapshotCacheSeconds     < 0) throw new ArgumentOutOfRangeException(nameof(QuickSnapshotCacheSeconds));
        if (ValidationRetries             < 0) throw new ArgumentOutOfRangeException(nameof(ValidationRetries));
        if (MaxConcurrentPairs            < 1) throw new ArgumentOutOfRangeException(nameof(MaxConcurrentPairs));
    }
}