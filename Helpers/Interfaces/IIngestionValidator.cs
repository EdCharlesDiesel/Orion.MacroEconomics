using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Helpers.Interfaces
{
    public interface IIngestionValidator
    {
        bool IsValid(EconomicIndicator indicator);

        /// <summary>
        /// Returns a list of validation errors. Empty list means valid.
        /// Default implementation derives an error list from <see cref="IsValid"/>.
        /// </summary>
        IReadOnlyList<string> Validate(EconomicIndicator indicator) =>
            IsValid(indicator)
                ? Array.Empty<string>()
                : new[] { "Indicator failed validation." };
    }
}
