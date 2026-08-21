using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using PetShop.Service.Common;

namespace PetShop.Api.Filters;

/// <summary>
/// Runs any registered FluentValidation validator against each action argument
/// before the action executes. Keeps controllers free of validation plumbing.
/// </summary>
public class ValidationFilter : IAsyncActionFilter
{
    private readonly IServiceProvider _services;
    private readonly ILogger<ValidationFilter> _logger;

    public ValidationFilter(IServiceProvider services, ILogger<ValidationFilter> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var errors = new List<string>();

        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null) continue;

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (_services.GetService(validatorType) is not IValidator validator) continue;

            var result = await validator.ValidateAsync(new ValidationContext<object>(argument));
            if (!result.IsValid)
                errors.AddRange(result.Errors.Select(e => e.ErrorMessage));
        }

        if (errors.Count > 0)
        {
            // A rejected request never reaches the service layer, so without this row the
            // log shows a request arriving and a 400 leaving with nothing in between.
            _logger.LogWarning("Validation failed for {Action}: {Errors:l}",
                context.ActionDescriptor.DisplayName, string.Join("; ", errors));

            context.Result = new BadRequestObjectResult(
                ApiResponse<object>.Fail("Validation failed.", errors));
            return;
        }

        await next();
    }
}
