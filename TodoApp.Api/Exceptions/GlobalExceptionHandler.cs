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
        int statusCode;
        string code;
        string message;
        List<FieldError>? details = null;

        switch (exception)
        {
            case ValidationException ex:
                statusCode = StatusCodes.Status400BadRequest;
                code = ex.Code;
                message = ex.Message;
                details = ex.Details.ToList();
                break;
            case NotFoundException ex:
                statusCode = StatusCodes.Status404NotFound;
                code = ex.Code;
                message = ex.Message;
                break;
            case UnauthorizedException ex:
                statusCode = StatusCodes.Status401Unauthorized;
                code = ex.Code;
                message = ex.Message;
                break;
            case ConflictException ex:
                statusCode = StatusCodes.Status409Conflict;
                code = ex.Code;
                message = ex.Message;
                break;
            default:
                statusCode = StatusCodes.Status500InternalServerError;
                code = "INTERNAL_ERROR";
                message = "An unexpected error occurred.";
                logger.LogError(exception, "Unhandled exception");
                break;
        }

        var correlationId = httpContext.Request.Headers["X-Correlation-Id"].FirstOrDefault()
            ?? Guid.NewGuid().ToString();

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(new ErrorResponse
        {
            Error = new ErrorDetail
            {
                Code = code,
                Message = message,
                Details = details,
                TraceId = httpContext.TraceIdentifier,
                CorrelationId = correlationId
            }
        }, cancellationToken);

        return true;
    }
}
