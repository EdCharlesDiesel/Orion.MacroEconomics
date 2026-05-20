namespace Orion.MacroEconomics.Configurations;

/// <summary>
/// Root application configuration. Bind this from appsettings.json.
/// </summary>
public sealed class AppConfiguration
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