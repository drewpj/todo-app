using System.Diagnostics;

namespace TodoApp.Api.Middleware;

public class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    public const string CorrelationIdKey = "CorrelationId";

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault()
            ?? Guid.NewGuid().ToString();

        context.Items[CorrelationIdKey] = correlationId;

        var sw = Stopwatch.StartNew();

        logger.LogInformation(
            "Request {Method} {Path} traceId={TraceId} correlationId={CorrelationId}",
            context.Request.Method,
            context.Request.Path,
            context.TraceIdentifier,
            correlationId);

        await next(context);

        sw.Stop();

        logger.LogInformation(
            "Response {Method} {Path} {StatusCode} {ElapsedMs}ms traceId={TraceId} correlationId={CorrelationId}",
            context.Request.Method,
            context.Request.Path,
            context.Response.StatusCode,
            sw.ElapsedMilliseconds,
            context.TraceIdentifier,
            correlationId);
    }
}
