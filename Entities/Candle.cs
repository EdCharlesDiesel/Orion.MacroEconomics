using Skender.Stock.Indicators;

namespace Orion.MacroEconomics.Entities
{
    public class Candle : IQuote
    {
        public string Pair { get; set; } = default!;

        public DateTime Time { get; set; }

        /// <summary>Alias for <see cref="Time"/>; required by <see cref="Skender.Stock.Indicators.IQuote"/>.</summary>
        public DateTime Date
        {
            get => Time;
            set => Time = value;
        }

        public decimal Open   { get; set; }
        public decimal High   { get; set; }
        public decimal Low    { get; set; }
        public decimal Close  { get; set; }
        public decimal Volume { get; set; }
    }
}
