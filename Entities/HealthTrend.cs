using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Orion.MacroEconomics.Entities;

public class HealthTrend
{
    public List<HealthSnapshot> Snapshots { get; set; } = new();
    public decimal UptimePercentage { get; set; }
    public TimeSpan MeanTimeToRecovery { get; set; }
    public Dictionary<HealthStatus, int> StatusDistribution { get; set; } = new();
}