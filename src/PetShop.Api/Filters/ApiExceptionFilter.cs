using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PetShop.Service.Common;

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

        _logger.LogWarning("Handled domain error: {Message}", appEx.Message);

        context.Result = new ObjectResult(ApiResponse<object>.Fail(appEx.Message))
        {
            StatusCode = appEx.StatusCode
        };
        context.ExceptionHandled = true;
    }
}
