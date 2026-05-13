using Orion.MacroEconomics.DTO;

namespace Orion.MacroEconomics.Free;

public interface IRegimeEngineFree
{
    RegimeResult Analyze(RegimeInput input);
}