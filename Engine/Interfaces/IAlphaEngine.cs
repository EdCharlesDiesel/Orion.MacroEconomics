using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Engine.Interfaces;

public interface IAlphaEngine
{
    AlphaResult Generate(string pair, List<NormalizedIndicator>? indicators, List<MacroEvent>? macroEvents = null);
    
}