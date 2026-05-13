namespace Orion.MacroEconomics.Configuration;

public sealed class AlphaVantageOptions
{
    public string ApiKey { get; set; } = "";
    public string BaseUrl { get; set; } = "https://www.alphavantage.co";
    public string DefaultInterval { get; set; } = "1d";
}

public sealed class GmailOptions
{
    public string FromEmail { get; set; } = "";
    public string ToEmail { get; set; } = "";
    public string AppPassword { get; set; } = "";
    public string SmtpHost { get; set; } = "smtp.gmail.com";
    public int SmtpPort { get; set; } = 587;
}