using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc.Filters;
using PetShop.Service.Common;
using Serilog.Context;

namespace PetShop.Api.Filters;

/// <summary>
/// Logs each action invocation with the elapsed time. Demonstrates a cross-cutting
/// action filter. This is action-scoped detail that complements (not replaces) the
/// request-level summary from UseSerilogRequestLogging, so it logs at Debug to avoid
/// duplicating the Information-level request line in normal operation.
/// <para>
/// Also the first point in the pipeline where the caller's identity is available AND
/// something still runs for every request, so it puts the user's email into the request
/// log context — every row written from here on carries it in the UserEmail column.
/// </para>
/// </summary>
public class RequestLoggingFilter : IAsyncActionFilter
{
    private readonly ILogger<RequestLoggingFilter> _logger;

    public RequestLoggingFilter(ILogger<RequestLoggingFilter> logger) => _logger = logger;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var user = context.HttpContext.User;
        RequestLogContext.Set(fields => fields.UserEmail ??=
            user.FindFirstValue(ClaimTypes.Email) ?? user.FindFirstValue("email"));

        var action = context.ActionDescriptor.DisplayName;
        var sw = Stopwatch.StartNew();

        var executed = await next();

        sw.Stop();
        var status = executed.HttpContext.Response.StatusCode;

        // Property named StatusCode (not Status) so it maps to the same HttpStatus column
        // as the request-summary line — one concept, one property name, one column.
        using (LogContext.PushProperty("EventType", "Action"))
            _logger.LogDebug("Action {Action} responded {StatusCode} in {Elapsed} ms",
                action, status, sw.ElapsedMilliseconds);
    }
}
