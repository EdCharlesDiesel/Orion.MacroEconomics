using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Data;



public sealed class MarketDataDocumentStore(IDocumentSession session) : IMarketDataDocumentStore
{
    public async Task StoreMarketDataAsync(
        string pair,
        object payload,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken = default)
    {
        session.Store(new MarketDataDocument
        {
            Provider = "AlphaVantage",
            Pair = pair.Trim().ToUpperInvariant(),
            DataType = "FX_DAILY",
            FromUtc = fromUtc,
            ToUtc = toUtc,
            Payload = payload
        });

        await session.SaveChangesAsync(cancellationToken);
    }

    public async Task StoreSignalAsync(
        TradingSignalDocument signal,
        CancellationToken cancellationToken = default)
    {
        session.Store(signal);
        await session.SaveChangesAsync(cancellationToken);
    }
}