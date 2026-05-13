using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using Orion.MacroEconomics.Configuration;
using Orion.MacroEconomics.Entities;

namespace Orion.MacroEconomics.Services;

public sealed class GmailSignalNotificationService(IOptions<GmailOptions> options,
    ILogger<GmailSignalNotificationService> logger) : IGmailSignalNotificationService
{
    private readonly GmailOptions _options = options.Value;

    public async Task SendSignalAsync(
        TradingSignalDocument signal,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.FromEmail))
            throw new InvalidOperationException("Gmail FromEmail is missing.");

        if (string.IsNullOrWhiteSpace(_options.ToEmail))
            throw new InvalidOperationException("Gmail ToEmail is missing.");

        if (string.IsNullOrWhiteSpace(_options.AppPassword))
            throw new InvalidOperationException("Gmail AppPassword is missing.");

        using var smtp = new SmtpClient(_options.SmtpHost, _options.SmtpPort)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(_options.FromEmail, _options.AppPassword)
        };

        using var message = new MailMessage
        {
            From = new MailAddress(_options.FromEmail),
            Subject = $"Orion Signal: {signal.Direction} {signal.Pair}",
            Body =
                $"""
                 Pair: {signal.Pair}
                 Direction: {signal.Direction}
                 Confidence: {signal.Confidence}%

                 Last Close: {signal.LastClose}
                 Fast SMA 20: {signal.FastSma}
                 Slow SMA 50: {signal.SlowSma}

                 Reason:
                 {signal.Reason}

                 Generated UTC:
                 {signal.CreatedUtc:O}
                 """,
            IsBodyHtml = false
        };

        message.To.Add(_options.ToEmail);

        await smtp.SendMailAsync(message, cancellationToken);

        logger.LogInformation(
            "Signal email sent to {Email} for {Pair}",
            _options.ToEmail,
            signal.Pair);
    }
}