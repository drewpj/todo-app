using Microsoft.AspNetCore.Diagnostics;
using TodoApp.Api.DTOs;
using TodoApp.Api.Middleware;

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
                break;
        }

        var correlationId = httpContext.Items[RequestLoggingMiddleware.CorrelationIdKey] as string
            ?? httpContext.Request.Headers["X-Correlation-Id"].FirstOrDefault()
            ?? Guid.NewGuid().ToString();

        var traceId = httpContext.TraceIdentifier;

        if (statusCode >= 500)
        {
            logger.LogError(
                exception,
                "Unhandled exception: {Code} {StatusCode} {Method} {Path} traceId={TraceId} correlationId={CorrelationId}",
                code, statusCode, httpContext.Request.Method, httpContext.Request.Path, traceId, correlationId);
        }
        else
        {
            logger.LogWarning(
                "{Code} {StatusCode} {Method} {Path} traceId={TraceId} correlationId={CorrelationId}",
                code, statusCode, httpContext.Request.Method, httpContext.Request.Path, traceId, correlationId);
        }

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(new ErrorResponse
        {
            Error = new ErrorDetail
            {
                Code = code,
                Message = message,
                Details = details,
                TraceId = traceId,
                CorrelationId = correlationId
            }
        }, cancellationToken);

        return true;
    }
}
