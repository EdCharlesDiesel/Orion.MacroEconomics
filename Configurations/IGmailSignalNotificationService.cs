using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Configurations;

public interface IGmailSignalNotificationService
{
    Task SendSignalAsync(TradingSignalDocument signal, CancellationToken cancellationToken = default);
}