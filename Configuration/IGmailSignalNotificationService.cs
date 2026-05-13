using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Configuration;

public interface IGmailSignalNotificationService
{
    Task SendSignalAsync(TradingSignalDocument signal, CancellationToken cancellationToken = default);
}