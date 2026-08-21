using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PetShop.Service.Common;
using Serilog.Context;

namespace PetShop.Api.Filters;

/// <summary>
/// Translates known <see cref="AppException"/> business errors into clean HTTP
/// responses. Unknown exceptions are left to the global ExceptionHandlingMiddleware.
/// </summary>
public class ApiExceptionFilter : IExceptionFilter
{
    private readonly ILogger<ApiExceptionFilter> _logger;

    public ApiExceptionFilter(ILogger<ApiExceptionFilter> logger) => _logger = logger;

    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not AppException appEx) return;

        // DomainError, not Exception: the request was refused by a business rule and the
        // client gets a 4xx. Filtering the two apart is the difference between "callers are
        // sending bad requests" and "we are broken".
        using (LogContext.PushProperty("EventType", "DomainError"))
            _logger.LogWarning("Handled domain error: {Message}", appEx.Message);

        context.Result = new ObjectResult(ApiResponse<object>.Fail(appEx.Message))
        {
            StatusCode = appEx.StatusCode
        };
        context.ExceptionHandled = true;
    }
}
