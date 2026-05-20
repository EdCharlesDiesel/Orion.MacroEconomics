namespace Orion.MacroEconomics.Engine.Interfaces
{
    public interface IExecutionCostModel
    {
        decimal EstimateSlippage(string pair, decimal size);
        decimal EstimateSpread(string pair, decimal bid, decimal ask);
    }
}
