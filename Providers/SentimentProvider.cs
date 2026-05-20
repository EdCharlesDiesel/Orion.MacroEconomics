using Orion.MacroEconomics.Entities;
using Orion.MacroEconomics.Providers.Interfaces;

namespace Orion.MacroEconomics.Providers
{
    public sealed class SentimentProvider : ISentimentDataProvider
    {
        public string Name => "Static";

        public Task<IReadOnlyList<SentimentItem>> GetSentimentItemsAsync(SentimentDataRequest request, CancellationToken cancellationToken = default)
        {
            var items = new List<SentimentItem>
            {
                new()
                {
                    Source = Name,
                    Title = $"{request.Pair} macro sentiment check",
                    Text = "Risk conditions are neutral with no strong directional headline bias.",
                    TimestampUtc = DateTime.UtcNow,
                    Weight = 1m
                }
            };

            return Task.FromResult<IReadOnlyList<SentimentItem>>(items);
        }
    }
}