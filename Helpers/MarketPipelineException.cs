namespace Orion.MacroEconomics.Helpers;

public class MarketPipelineException : Exception
{
    public MarketPipelineException()
        : base("A market pipeline error occurred.")
    {
    }

    public MarketPipelineException(string message)
        : base(message)
    {
    }

    public MarketPipelineException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}