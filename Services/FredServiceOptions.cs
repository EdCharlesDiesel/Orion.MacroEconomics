namespace Orion.MacroEconomics.Services;

public class FredServiceOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public int MaxRetries { get; set; } = 3;
    public int CircuitBreakerThreshold { get; set; } = 5;
    public int CircuitBreakerDurationSeconds { get; set; } = 60;
    public int RequestTimeoutSeconds { get; set; } = 30;
    public int MaxConcurrentRequests { get; set; } = 3;
    public int CacheExpirationSeconds { get; set; } = 3600; // 1 hour
    public int CacheSlidingExpirationSeconds { get; set; } = 1800; // 30 minutes
    public int StaleDataThresholdSeconds { get; set; } = 5400; // 1.5 hours
}

public record FredSeriesInfo(string SeriesId, string Description, string Unit, Frequency Frequency);

public enum Frequency
{
    Daily,
    Weekly,
    Monthly,
    Quarterly,
    Annual
}

