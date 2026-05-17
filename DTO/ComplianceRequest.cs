using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.DTO;

public class ComplianceRequest
{
    public string? Pair { get; set; }
    public string? Direction { get; set; }
    public int RequestedSize { get; set; }
    public AccountSnapshot Account { get; set; }
    public RealTimeRiskResult Risk { get; set; }
}