using Orion.MacroEconomics.Entities;
using Orion.MacroEconomics.Helpers.Interfaces;

namespace Orion.MacroEconomics.Helpers;

public sealed class IngestionValidator : IIngestionValidator
{
    public bool IsValid(EconomicIndicator indicator)
    {
        return Validate(indicator).Count == 0;
    }

    public IReadOnlyList<string> Validate(EconomicIndicator indicator)
    {
        var errors = new List<string>();

        if (indicator == null)
        {
            errors.Add("Indicator is null.");
            return errors;
        }

        if (string.IsNullOrWhiteSpace(indicator.Country))
            errors.Add("Country is required.");

        if (string.IsNullOrWhiteSpace(indicator.Indicator))
            errors.Add("Indicator name is required.");

        if (!indicator.Value.HasValue)
            errors.Add("Value is required.");

        if (indicator.Date == default)
            errors.Add("Date is required.");

        if (indicator.Date > DateTime.UtcNow.AddDays(1))
            errors.Add("Date cannot be in the future.");

        if (string.IsNullOrWhiteSpace(indicator.Frequency))
            errors.Add("Frequency is required.");

        return errors;
    }
}
