namespace Orion.MacroEconomics.Configurations;

/// <summary>
/// Root settings object bound from <c>appsettings.json</c> via <see cref="Microsoft.Extensions.Options.IOptions{TOptions}"/>.
/// Kept as a distinct type from <see cref="AppConfiguration"/> so legacy <c>IOptions&lt;AppSettings&gt;</c> bindings remain valid.
/// </summary>
public sealed class AppSettings
{
    public string Version { get; set; } = "1.0.0";

    public string ApiBaseUrl { get; set; } = string.Empty;

    public int CacheTtlSeconds { get; set; } = 300;

    public int AutoRefreshIntervalSeconds { get; set; } = 300;

    public RiskSettings Risk { get; set; } = new();

    public IndicatorSettings Indicators { get; set; } = new();

    public EmailSettings Email { get; set; } = new();

    public LiveTradingConfig? LiveTrading { get; set; }
}
