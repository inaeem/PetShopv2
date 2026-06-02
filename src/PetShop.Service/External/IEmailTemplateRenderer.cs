namespace PetShop.Service.External;

/// <summary>
/// Renders an email from a named template on disk. Templates live under
/// <see cref="MailSettings.TemplatesPath"/>, one folder per template, each containing a
/// <c>subject.txt</c> and a body file (<c>body.html</c> preferred, else <c>body.txt</c>).
/// Tokens written as <c>{{Key}}</c> are replaced from the supplied values.
/// <para>
/// Unlike sending, a missing/malformed template is a configuration error, so rendering
/// throws rather than returning a best-effort result.
/// </para>
/// </summary>
public interface IEmailTemplateRenderer
{
    Task<RenderedEmail> RenderAsync(
        string templateName,
        IReadOnlyDictionary<string, string?> tokens,
        CancellationToken ct = default);
}

/// <summary>Subject/body produced from a template, ready to wrap in an <see cref="EmailMessage"/>.</summary>
public record RenderedEmail(string Subject, string Body, bool IsHtml);
