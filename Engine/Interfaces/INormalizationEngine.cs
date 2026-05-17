using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Engine.Interfaces
{

    namespace Orion.API.TradingEconomics.Engine.Interfaces
    {
        public interface INormalizationEngine
        {
            List<NormalizedIndicator> Normalize(IEnumerable<EconomicIndicator> raw);
        }
    }
}
