using System.Net;
using System.Net.Mail;

namespace Vault.Core;

/// <summary>
/// Core SMTP email sender used by VaultService. This class handles all outbound email
/// transport using the configured SMTP credentials.
/// </summary>
public class EmailSender
{
    private readonly string _smtpUser;
    private readonly string _smtpPass;
    private readonly string _smtpHost;
    private readonly int _smtpPort;

    /// <summary>
    /// Default recipients loaded from configuration.
    /// </summary>
    public IReadOnlyList<string> DefaultRecipients { get; }

    public EmailSender(
        string smtpUser,
        string smtpPass,
        string smtpHost,
        int smtpPort,
        IEnumerable<string> defaultRecipients
    )
    {
        _smtpUser = smtpUser;
        _smtpPass = smtpPass;
        _smtpHost = smtpHost;
        _smtpPort = smtpPort;

        DefaultRecipients = defaultRecipients?.ToList()
            ?? [];
    }

    // ------------------------------------------------------------
    // SEND HTML EMAIL (PRIMARY)
    // ------------------------------------------------------------
    public async Task SendHtmlEmailAsync(IEnumerable<string> to, string subject, string htmlBody)
    {
        using var client = new SmtpClient(_smtpHost, _smtpPort)
        {
            EnableSsl = true,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(_smtpUser, _smtpPass)
        };

        var message = new MailMessage
        {
            From = new MailAddress(_smtpUser),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };

        foreach (var addr in to)
        {
            if (!string.IsNullOrWhiteSpace(addr))
                message.To.Add(addr.Trim());
        }

        await client.SendMailAsync(message);
    }

    /// <summary>
    /// Sends an HTML email to the default recipients.
    /// </summary>
    public async Task SendHtmlEmailAsync(string subject, string htmlBody)
    {
        await SendHtmlEmailAsync(DefaultRecipients, subject, htmlBody);
    }

    // ------------------------------------------------------------
    // SEND PLAIN TEXT EMAIL (FALLBACK)
    // ------------------------------------------------------------
    public async Task SendPlainTextEmailAsync(IEnumerable<string> to, string subject, string body)
    {
        using var client = new SmtpClient(_smtpHost, _smtpPort)
        {
            EnableSsl = true,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential(_smtpUser, _smtpPass)
        };

        var message = new MailMessage
        {
            From = new MailAddress(_smtpUser),
            Subject = subject,
            Body = body,
            IsBodyHtml = false
        };

        foreach (var addr in to)
        {
            if (!string.IsNullOrWhiteSpace(addr))
                message.To.Add(addr.Trim());
        }

        await client.SendMailAsync(message);
    }

    /// <summary>
    /// Sends a plain text email to the default recipients.
    /// </summary>
    public async Task SendPlainTextEmailAsync(string subject, string body)
    {
        await SendPlainTextEmailAsync(DefaultRecipients, subject, body);
    }
}