using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Helpers.Interfaces
{
    public interface IIngestionValidator
    {
        bool IsValid(EconomicIndicator indicator);
    }
}
