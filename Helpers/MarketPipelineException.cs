namespace Orion.MacroEconomics.Helpers;

public class MarketPipelineException : Exception
{
    public MarketPipelineException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
}