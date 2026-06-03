namespace PetShop.Service.External;

/// <summary>
/// Sends outbound email. Best-effort, like <see cref="IPetSyncClient"/>: implementations
/// never throw for SMTP/transport errors (those are reported on <see cref="EmailResult"/>),
/// so a delivery failure cannot fail the calling operation. Genuine caller cancellation is
/// still propagated.
/// </summary>
public interface IEmailSender
{
    Task<EmailResult> SendAsync(EmailMessage message, CancellationToken ct = default);
}

/// <summary>A single outbound email.</summary>
/// <param name="To">Recipient address.</param>
/// <param name="Subject">Subject line.</param>
/// <param name="Body">Message body (plain text unless <paramref name="IsHtml"/> is true).</param>
/// <param name="IsHtml">Whether <paramref name="Body"/> is HTML.</param>
public record EmailMessage(string To, string Subject, string Body, bool IsHtml = false);

/// <summary>Outcome of a send attempt.</summary>
/// <param name="Success">True when SMTP accepted the message.</param>
/// <param name="Skipped">True when mail is disabled, so no send was attempted.</param>
/// <param name="Error">Failure reason when <paramref name="Success"/> is false.</param>
public record EmailResult(bool Success, bool Skipped, string? Error)
{
    public static EmailResult Disabled() => new(false, true, null);
    public static EmailResult Ok() => new(true, false, null);
    public static EmailResult Failed(string error) => new(false, false, error);
}
