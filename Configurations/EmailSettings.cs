namespace Orion.MacroEconomics.Configurations;
public sealed class EmailSettings
{
    public string SmtpHost { get; set; } = "smtp.gmail.com";

    public int SmtpPort { get; set; } = 587;

    public string SmtpUser { get; set; } = string.Empty;

    public string SmtpPassword { get; set; } = string.Empty;

    public string Sender { get; set; } = string.Empty;

    public string Recipient { get; set; } = string.Empty;
}