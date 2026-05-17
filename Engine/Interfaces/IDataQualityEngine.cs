using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Engine.Interfaces
{
    public interface IDataQualityEngine
    {
        DataQualityResult ValidateCandles(IReadOnlyList<OhlcvBar> candles);
    }
}
