using CommunityIncidentReporting.Api.Contracts;
using CommunityIncidentReporting.Application.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace CommunityIncidentReporting.Api.Middleware;

/// <summary>
/// Catches every unhandled exception, logs it, and returns the consistent
/// { "error": { code, message, details? } } envelope instead of leaking a stack trace.
/// Registered via AddExceptionHandler + UseExceptionHandler in Program.cs.
/// </summary>
public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, code) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "not_found"),
            ForbiddenAccessException => (StatusCodes.Status403Forbidden, "forbidden"),
            BusinessRuleException => (StatusCodes.Status409Conflict, "business_rule_violation"),
            _ => (StatusCodes.Status500InternalServerError, "internal_error")
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception for {Method} {Path}",
                httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            logger.LogWarning("{ExceptionType} for {Method} {Path}: {Message}",
                exception.GetType().Name, httpContext.Request.Method, httpContext.Request.Path, exception.Message);
        }

        var message = statusCode == StatusCodes.Status500InternalServerError
            ? "An unexpected error occurred. Please try again or contact support if the problem persists."
            : exception.Message;

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(
            new ErrorResponse(new ErrorBody(code, message)), cancellationToken);

        return true;
    }
}
