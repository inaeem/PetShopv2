using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PetShop.Service.External;

/// <summary>
/// <see cref="IEmailSender"/> backed by <see cref="System.Net.Mail.SmtpClient"/>. Reads the
/// SMTP host/credentials from <see cref="MailSettings"/>. Best-effort: transport failures
/// are caught and returned as <see cref="EmailResult.Failed"/> rather than thrown.
/// </summary>
public class SmtpEmailSender : IEmailSender
{
    private readonly MailSettings _settings;
    private readonly ILogger<SmtpEmailSender> _logger;

    public SmtpEmailSender(IOptions<MailSettings> settings, ILogger<SmtpEmailSender> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<EmailResult> SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        if (!_settings.Enabled)
        {
            _logger.LogDebug("Mail disabled; skipping email to {To}", message.To);
            return EmailResult.Disabled();
        }

        try
        {
            using var smtp = new SmtpClient(_settings.Host, _settings.Port)
            {
                EnableSsl = _settings.UseSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                Timeout = _settings.TimeoutSeconds * 1000
            };

            // Leave Credentials null for an unauthenticated relay.
            if (!string.IsNullOrWhiteSpace(_settings.Username))
                smtp.Credentials = new NetworkCredential(_settings.Username, _settings.Password);

            using var mail = new MailMessage
            {
                From = new MailAddress(_settings.FromAddress, _settings.FromName),
                Subject = message.Subject,
                Body = message.Body,
                IsBodyHtml = message.IsHtml
            };
            mail.To.Add(message.To);

            await smtp.SendMailAsync(mail, ct);

            _logger.LogInformation("Sent email to {To} ({Subject})", message.To, message.Subject);
            return EmailResult.Ok();
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw; // honour genuine caller cancellation
        }
        catch (Exception ex)
        {
            // Best-effort: report, don't throw, so callers can decide what a failure means.
            _logger.LogWarning(ex, "Failed to send email to {To}", message.To);
            return EmailResult.Failed(ex.Message);
        }
    }
}
