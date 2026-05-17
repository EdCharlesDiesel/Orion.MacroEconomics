// using System.Net.Mail;
// using Microsoft.Extensions.Options;
// using MimeKit;
// using Orion.MacroEconomics.Configurations;
// using Orion.MacroEconomics.Entities;
//
// namespace Orion.MacroEconomics.Services;
//
// public class EmailAlertService(IOptions<AppSettings> opts, ILogger<EmailAlertService> log)
// {
//     private readonly EmailSettings           _cfg = opts.Value.Email;
//
//     public async Task SendSignalAlertsAsync(IReadOnlyList<TradeSignal> signals, CancellationToken ct = default)
//     {
//         if (signals.Count == 0) return;
//         if (string.IsNullOrEmpty(_cfg.SmtpUser) || string.IsNullOrEmpty(_cfg.Recipient))
//         {
//             log.LogWarning("Email not configured — skipping alert");
//             return;
//         }
//
//         var subject = $"🚨 {signals.Count} New Signal{(signals.Count > 1 ? "s" : "")} — "
//                     + string.Join(", ", signals.Take(3).Select(s => s.Pair))
//                     + (signals.Count > 3 ? "…" : "");
//
//         var message = new MimeMessage();
//         message.From.Add(MailboxAddress.Parse(_cfg.Sender));
//         message.To.Add(MailboxAddress.Parse(_cfg.Recipient));
//         message.Subject = subject;
//
//         var builder = new BodyBuilder
//         {
//             HtmlBody  = BuildHtml(signals),
//             TextBody  = BuildPlainText(signals),
//         };
//         message.Body = builder.ToMessageBody();
//
//         try
//         {
//             using var client = new SmtpClient();
//             await client.ConnectAsync(_cfg.SmtpHost, _cfg.SmtpPort, SecureSocketOptions.StartTls, ct);
//             await client.AuthenticateAsync(_cfg.SmtpUser, _cfg.SmtpPassword, ct);
//             await client.SendAsync(message, ct);
//             await client.DisconnectAsync(true, ct);
//             log.LogInformation("Signal email sent to {Recipient} ({Count} signals)",
//                                 _cfg.Recipient, signals.Count);
//         }
//         catch (Exception ex)
//         {
//             log.LogError(ex, "Failed to send signal email");
//         }
//     }
//
//
//
//     private static string BuildHtml(IReadOnlyList<TradeSignal> signals)
//     {
//         var rows = string.Concat(signals.Select(s =>
//         {
//             var dir   = s.Bias == "Long" ? "📈 LONG" : "📉 SHORT";
//             var color = s.Bias == "Long" ? "#26a69a" : "#ef5350";
//             return $"""
//                 <tr style="border-bottom:1px solid #2d3148">
//                   <td style="padding:10px 14px;font-weight:700;color:#e0e0e0">{s.Pair}</td>
//                   <td style="padding:10px 14px;color:{color};font-weight:700">{dir}</td>
//                   <td style="padding:10px 14px;color:#e0e0e0">{s.Entry:F5}</td>
//                   <td style="padding:10px 14px;color:#26a69a">{s.TakeProfit1:F5}
//                     <span style="font-size:11px;color:#8b8fa8"> R:R {s.RiskReward1:F2}</span></td>
//                   <td style="padding:10px 14px;color:#ef5350">{s.StopLoss:F5}</td>
//                   <td style="padding:10px 14px;color:#ffa726">{s.StrengthScore}/10</td>
//                   <td style="padding:10px 14px;color:#c0c0c0">{s.Conviction}</td>
//                 </tr>
//                 <tr>
//                   <td colspan="7" style="padding:4px 14px 12px;color:#8b8fa8;font-size:12px">
//                     {System.Web.HttpUtility.HtmlEncode(s.Thesis.Count > 180 ? s.Thesis[..180] : s.Thesis)}
//                   </td>
//                 </tr>
//                 """;
//         }));
//
//         return $"""
//             <!DOCTYPE html>
//             <html>
//             <body style="background:#0e1117;font-family:'Courier New',monospace;color:#c0c0c0;margin:0;padding:20px">
//               <div style="max-width:760px;margin:0 auto">
//                 <h2 style="color:#4af0c4;border-bottom:1px solid #2d3148;padding-bottom:10px">
//                   🚨 Macro Dashboard — New Trading Signals
//                 </h2>
//                 <p style="color:#8b8fa8;font-size:13px">
//                   {signals.Count} new signal{(signals.Count > 1 ? "s" : "")} at
//                   <strong style="color:#e0e0e0">{DateTime.Now:yyyy-MM-dd HH:mm:ss}</strong>
//                 </p>
//                 <table style="width:100%;border-collapse:collapse;background:#1a1d27;border-radius:8px;overflow:hidden">
//                   <thead>
//                     <tr style="background:#0d1117">
//                       <th style="padding:10px 14px;text-align:left;color:#8b8fa8;font-size:11px">PAIR</th>
//                       <th style="padding:10px 14px;text-align:left;color:#8b8fa8;font-size:11px">BIAS</th>
//                       <th style="padding:10px 14px;text-align:left;color:#8b8fa8;font-size:11px">ENTRY</th>
//                       <th style="padding:10px 14px;text-align:left;color:#8b8fa8;font-size:11px">TP1</th>
//                       <th style="padding:10px 14px;text-align:left;color:#8b8fa8;font-size:11px">STOP</th>
//                       <th style="padding:10px 14px;text-align:left;color:#8b8fa8;font-size:11px">SCORE</th>
//                       <th style="padding:10px 14px;text-align:left;color:#8b8fa8;font-size:11px">CONVICTION</th>
//                     </tr>
//                   </thead>
//                   <tbody>{rows}</tbody>
//                 </table>
//                 <p style="color:#8b8fa8;font-size:11px;margin-top:24px;border-top:1px solid #2d3148;padding-top:12px">
//                   Macro Dashboard Pro · Alerts fire for High conviction or score ≥ 8 · Each unique entry fires once.
//                 </p>
//               </div>
//             </body>
//             </html>
//             """;
//     }
//
//     private static string BuildPlainText(IReadOnlyList<TradeSignal> signals)
//     {
//         var lines = new List<string> { $"NEW TRADING SIGNALS — {DateTime.Now:yyyy-MM-dd HH:mm}", "" };
//         foreach (var s in signals)
//         {
//             lines.Add($"{s.Pair} {s.Bias.ToUpper()} | Entry:{s.Entry:F5} TP1:{s.TakeProfit1:F5} SL:{s.StopLoss:F5} Score:{s.StrengthScore}/10 {s.Conviction}");
//             lines.Add($"  {s.Thesis[..Math.Min(160, s.Thesis.Count)]}");
//             lines.Add("");
//         }
//         return string.Join("\n", lines);
//     }
// }
//
//
