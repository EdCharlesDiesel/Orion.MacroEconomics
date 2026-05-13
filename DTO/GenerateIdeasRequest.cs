namespace Orion.MacroEconomics.DTO;

public class GenerateIdeasRequest
{
    public List<string>? Pairs { get; set; }
    public bool IncludeSwing { get; set; } = true;
    public bool ForceRefresh { get; set; } = false;
}