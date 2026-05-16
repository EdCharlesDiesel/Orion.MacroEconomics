using Orion.MacroEconomics.Entities;
using Orion.MacroEconomics.Models;

namespace Orion.MacroEconomics.Providers.Interfaces;

public interface IMassiveDataProvider
{
    public Task<List<Candle>> GetCandlesAsync(string pair, string timeframe, CancellationToken ct = default);
    public Task<(decimal Price, decimal ChangePercent)> GetSnapshotAsync(string pair, CancellationToken ct = default);
    public Task<(List<MacroSnapshot> Data, bool IsLive)> GetAllMacroAsync(CancellationToken ct = default);
    Task<List<OhlcvBar>> FetchDataAsync(string pair, string s, string s1, CancellationToken cancellationToken);
}