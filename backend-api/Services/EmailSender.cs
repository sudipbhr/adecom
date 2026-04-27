using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace WeatherAPI.Services;

/// <summary>Simple settings bound from appsettings.json "Email" section.</summary>
public class EmailOptions
{
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string From { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool EnableSsl { get; set; } = true;
}

/// <summary>Abstraction used throughout the app to send emails.</summary>
public interface IEmailSender
{
    Task SendEmailAsync(string to, string subject, string htmlBody);
}

/// <summary>
/// SMTP implementation of IEmailSender.
/// Configure credentials in appsettings.json under the "Email" section.
/// </summary>
public class SmtpEmailSender : IEmailSender
{
    private readonly EmailOptions _opts;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<EmailOptions> opts, ILogger<SmtpEmailSender> logger)
    {
        _opts   = opts.Value;
        _logger = logger;
    }

    public async Task SendEmailAsync(string to, string subject, string htmlBody)
    {
        using var client = new SmtpClient(_opts.SmtpHost, _opts.SmtpPort)
        {
            EnableSsl   = _opts.EnableSsl,
            Credentials = new NetworkCredential(_opts.From, _opts.Password)
        };

        var message = new MailMessage(_opts.From, to, subject, htmlBody)
        {
            IsBodyHtml = true
        };

        try
        {
            await client.SendMailAsync(message);
        }
        catch (Exception ex)
        {
            // Log but do not throw – registration should not fail if SMTP is down.
            _logger.LogError(ex, "Failed to send email to {To}", to);
        }
    }
}
