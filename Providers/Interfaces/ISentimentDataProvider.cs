using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Providers.Interfaces
{
    public interface ISentimentDataProvider
    {
        string Name { get; }

        Task<IReadOnlyList<SentimentItem>> GetSentimentItemsAsync(
            SentimentDataRequest request,
            CancellationToken cancellationToken = default);
    }
}
