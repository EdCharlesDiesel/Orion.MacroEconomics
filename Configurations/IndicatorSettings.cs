namespace Orion.MacroEconomics.Configurations;

/// <summary>
/// Indicator calculation parameters. These are algorithm-level tunables,
/// not market conventions — keep them in config so strategies can be
/// adjusted without recompiling.
/// </summary>
public sealed class IndicatorSettings
{
    public int AtrPeriod { get; set; } = 14;

    public int RsiPeriod { get; set; } = 14;

    public int ShortEmaPeriod { get; set; } = 20;

    public int LongEmaPeriod { get; set; } = 50;

    public int KeyLevelLookback { get; set; } = 20;
}