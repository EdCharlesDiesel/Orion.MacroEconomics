using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Interfaces
{
    public interface IIngestionValidator
    {
        bool IsValid(EconomicIndicator indicator);
    }
}
