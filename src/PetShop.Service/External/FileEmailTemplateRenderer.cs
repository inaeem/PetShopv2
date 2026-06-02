using Microsoft.Extensions.Options;

namespace PetShop.Service.External;

/// <summary>
/// <see cref="IEmailTemplateRenderer"/> that loads templates from the file system, one
/// folder per template under <see cref="MailSettings.TemplatesPath"/>:
/// <code>
/// &lt;TemplatesPath&gt;/&lt;templateName&gt;/subject.txt   (required)
/// &lt;TemplatesPath&gt;/&lt;templateName&gt;/body.html     (preferred body)
/// &lt;TemplatesPath&gt;/&lt;templateName&gt;/body.txt      (fallback body)
/// </code>
/// </summary>
public class FileEmailTemplateRenderer : IEmailTemplateRenderer
{
    private readonly string _root;

    public FileEmailTemplateRenderer(IOptions<MailSettings> settings)
    {
        var configured = settings.Value.TemplatesPath;
        // Relative paths resolve against the app base dir (where the templates are copied
        // on build); absolute paths are honoured as-is.
        _root = Path.IsPathRooted(configured)
            ? configured
            : Path.Combine(AppContext.BaseDirectory, configured);
    }

    public async Task<RenderedEmail> RenderAsync(
        string templateName,
        IReadOnlyDictionary<string, string?> tokens,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(templateName))
            throw new ArgumentException("Template name is required.", nameof(templateName));

        // Guard against path traversal: a template name is a single folder, not a path.
        if (templateName.IndexOfAny(new[] { '/', '\\' }) >= 0 || templateName.Contains(".."))
            throw new ArgumentException($"Invalid template name '{templateName}'.", nameof(templateName));

        var dir = Path.Combine(_root, templateName);
        if (!Directory.Exists(dir))
            throw new DirectoryNotFoundException($"Email template '{templateName}' not found at '{dir}'.");

        var subjectPath = Path.Combine(dir, "subject.txt");
        if (!File.Exists(subjectPath))
            throw new FileNotFoundException($"Email template '{templateName}' is missing subject.txt.", subjectPath);

        var htmlPath = Path.Combine(dir, "body.html");
        var textPath = Path.Combine(dir, "body.txt");
        var isHtml = File.Exists(htmlPath);
        var bodyPath = isHtml ? htmlPath : textPath;
        if (!File.Exists(bodyPath))
            throw new FileNotFoundException(
                $"Email template '{templateName}' is missing body.html/body.txt.", bodyPath);

        var subject = Replace(await File.ReadAllTextAsync(subjectPath, ct), tokens).Trim();
        var body = Replace(await File.ReadAllTextAsync(bodyPath, ct), tokens);

        return new RenderedEmail(subject, body, isHtml);
    }

    // Replaces every {{Key}} occurrence with its value (null -> empty). Unmatched
    // {{...}} placeholders are left untouched so they surface in testing.
    private static string Replace(string template, IReadOnlyDictionary<string, string?> tokens)
    {
        if (tokens.Count == 0)
            return template;

        foreach (var (key, value) in tokens)
            template = template.Replace("{{" + key + "}}", value ?? string.Empty);

        return template;
    }
}
