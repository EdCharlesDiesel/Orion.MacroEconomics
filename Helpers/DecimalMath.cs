namespace Orion.MacroEconomics.Helpers;

/// <summary>
/// Decimal-friendly wrappers around <see cref="System.Math"/>.
/// Internally promotes to <see cref="double"/> for the transcendental call and casts the result back to <see cref="decimal"/>.
/// </summary>
public static class DecimalMath
{
    public static decimal Sqrt(decimal value) =>
        (decimal)System.Math.Sqrt((double)value);

    public static decimal Pow(decimal value, decimal exponent) =>
        (decimal)System.Math.Pow((double)value, (double)exponent);

    public static decimal Log(decimal value) =>
        (decimal)System.Math.Log((double)value);

    public static decimal Sin(decimal value) =>
        (decimal)System.Math.Sin((double)value);

    public static decimal Cos(decimal value) =>
        (decimal)System.Math.Cos((double)value);

    public static decimal Exp(decimal value) =>
        (decimal)System.Math.Exp((double)value);

    public const decimal Pi = 3.1415926535897932384626433833m;
}
