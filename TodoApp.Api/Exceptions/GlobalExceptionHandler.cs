using Microsoft.AspNetCore.Diagnostics;
using TodoApp.Api.DTOs;

namespace TodoApp.Api.Exceptions;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, code, message) = exception switch
        {
            NotFoundException ex    => (StatusCodes.Status404NotFound,    ex.Code, ex.Message),
            UnauthorizedException ex => (StatusCodes.Status401Unauthorized, ex.Code, ex.Message),
            ConflictException ex    => (StatusCodes.Status409Conflict,    ex.Code, ex.Message),
            _                       => (StatusCodes.Status500InternalServerError, "INTERNAL_ERROR", "An unexpected error occurred.")
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Unhandled exception");

        var correlationId = httpContext.Request.Headers["X-Correlation-Id"].FirstOrDefault()
            ?? Guid.NewGuid().ToString();

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(new ErrorResponse
        {
            Error = new ErrorDetail
            {
                Code = code,
                Message = message,
                TraceId = httpContext.TraceIdentifier,
                CorrelationId = correlationId
            }
        }, cancellationToken);

        return true;
    }
}
