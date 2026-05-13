using System.Text.Json;
using Orion.MacroEconomics.Entities;
using Orion.MacroEconomics.Interfaces;

namespace Orion.MacroEconomics.Data;

public sealed class MarketDataStore(TradingDbContext db) : IMarketDataStore
{
    public async Task<MarketDataSnapshot> SaveAsync(string provider, string dataType, string symbol, DateTime fromUtc, DateTime toUtc, object payload, CancellationToken cancellationToken = default)
    {
        var json = JsonSerializer.Serialize(payload);
        var snapshot = new MarketDataSnapshot
        {
            Provider = provider,
            DataType = dataType,
            Symbol = symbol.ToUpperInvariant(),
            FromUtc = fromUtc,
            ToUtc = toUtc,
            Payload = JsonDocument.Parse(json)
        };

        db.MarketDataSnapshots.Add(snapshot);
        await db.SaveChangesAsync(cancellationToken);

        return snapshot;
    }
}