namespace Orion.MacroEconomics.Helpers;

public class MarketPipelineStats
{
    public int CachedPairsCount { get; set; }
    public decimal CacheHitRate { get; set; }
    public decimal AverageLoadTimeMs { get; set; }
    public long TotalRequests { get; set; }
    public long SuccessfulRequests { get; set; }
    public long FailedRequests { get; set; }
    public long RejectedRequests { get; set; }

    public decimal SuccessRate => TotalRequests > 0
        ? (decimal)SuccessfulRequests / TotalRequests * 100m
        : 0m;

    public decimal FailureRate => TotalRequests > 0
        ? (decimal)FailedRequests / TotalRequests * 100m
        : 0m;

    public decimal RejectionRate => TotalRequests > 0
        ? (decimal)RejectedRequests / TotalRequests * 100m
        : 0m;

    public decimal CacheHitRatePercent => CacheHitRate * 100m;

    public TimeSpan AverageLoadTime => TimeSpan.FromMilliseconds((double)AverageLoadTimeMs);

    public bool HasCachedData => CachedPairsCount > 0;

    public override string ToString() =>
        $"Requests: {TotalRequests} (ok={SuccessfulRequests}, fail={FailedRequests}, rejected={RejectedRequests}) | " +
        $"Success={SuccessRate:F1}% Failure={FailureRate:F1}% Rejection={RejectionRate:F1}% | " +
        $"CachedPairs={CachedPairsCount} CacheHit={CacheHitRatePercent:F1}% AvgLoad={AverageLoadTimeMs:F1}ms";
}
