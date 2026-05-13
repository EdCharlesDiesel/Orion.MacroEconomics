namespace Orion.MacroEconomics.DTO
{
    /// <summary>
    /// FRED API status response
    /// </summary>
    public class FredStatusResponse
    {
        public bool IsConfigured { get; set; }
        public string ApiKeyProvided { get; set; }
        public bool IsConnected { get; set; }
        public string Message { get; set; }
        public DateTime CheckedAtUtc { get; set; }
        public Dictionary<string, bool> SeriesAvailability { get; set; }
        public int? RateLimitRemaining { get; set; }
        public string Source { get; set; }
    }
}