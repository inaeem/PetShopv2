using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Filters;

namespace PetShop.Api.Filters;

/// <summary>
/// Logs each action invocation with the elapsed time. Demonstrates a cross-cutting
/// action filter. This is action-scoped detail that complements (not replaces) the
/// request-level summary from UseSerilogRequestLogging, so it logs at Debug to avoid
/// duplicating the Information-level request line in normal operation.
/// </summary>
public class RequestLoggingFilter : IAsyncActionFilter
{
    private readonly ILogger<RequestLoggingFilter> _logger;

    public RequestLoggingFilter(ILogger<RequestLoggingFilter> logger) => _logger = logger;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var action = context.ActionDescriptor.DisplayName;
        var sw = Stopwatch.StartNew();

        var executed = await next();

        sw.Stop();
        var status = executed.HttpContext.Response.StatusCode;
        _logger.LogDebug("Action {Action} responded {Status} in {Elapsed} ms",
            action, status, sw.ElapsedMilliseconds);
    }
}
