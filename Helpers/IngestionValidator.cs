using Orion.MacroEconomics.Entities;
using Orion.MacroEconomics.Helpers.Interfaces;

namespace Orion.MacroEconomics.Helpers;
public sealed class IngestionValidator : IIngestionValidator
{
    public bool IsValid(EconomicIndicator indicator)
    {
        if (indicator == null)
            return false;

        if (string.IsNullOrWhiteSpace(indicator.Country))
            return false;

        if (string.IsNullOrWhiteSpace(indicator.Indicator))
            return false;

        if (!indicator.Value.HasValue)
            return false;

        return true;
    }
}