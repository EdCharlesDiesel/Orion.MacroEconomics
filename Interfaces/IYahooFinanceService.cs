using System.Collections;
using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Controllers;

public interface IYahooFinanceService
{
    Task<MarketDataResponse> FetchDataAsync(string pair, string tfConfigInterval, string tfConfigPeriod, CancellationToken cancellationToken);
    Task<IEnumerable> FetchAllTimeframesAsync(string pair, CancellationToken cancellationToken);
}