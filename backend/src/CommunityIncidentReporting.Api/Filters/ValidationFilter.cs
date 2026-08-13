using CommunityIncidentReporting.Api.Contracts;
using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CommunityIncidentReporting.Api.Filters;

/// <summary>
/// Runs a FluentValidation IValidator&lt;T&gt; (if one is registered) against every action
/// argument before the action executes, short-circuiting with a 422 + validation_error
/// envelope on failure. Registered globally in Program.cs so controllers never need to
/// call validators manually.
/// </summary>
public class ValidationFilter(IServiceProvider serviceProvider) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
            {
                continue;
            }

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (serviceProvider.GetService(validatorType) is not IValidator validator)
            {
                continue;
            }

            var validationContext = new ValidationContext<object>(argument);
            var result = await validator.ValidateAsync(validationContext, context.HttpContext.RequestAborted);

            if (!result.IsValid)
            {
                var details = result.Errors
                    .GroupBy(e => e.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

                context.Result = new Microsoft.AspNetCore.Mvc.ObjectResult(
                    new ErrorResponse(new ErrorBody("validation_error", "One or more fields are invalid.", details)))
                {
                    StatusCode = StatusCodes.Status422UnprocessableEntity
                };
                return;
            }
        }

        await next();
    }
}
