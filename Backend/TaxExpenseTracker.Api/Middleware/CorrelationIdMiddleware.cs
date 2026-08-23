namespace TaxExpenseTracker.Api.Middleware;

public sealed class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-ID";

    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var incoming = context.Request.Headers[HeaderName].FirstOrDefault();
        var correlationId = string.IsNullOrWhiteSpace(incoming)
            ? Guid.NewGuid().ToString("N")
            : incoming.Trim();

        context.TraceIdentifier = correlationId;
        context.Response.Headers[HeaderName] = correlationId;

        var startedAt = TimeProvider.System.GetTimestamp();
        await _next(context);
        _logger.LogInformation(
            "HTTP request completed. method={Method} path={Path} statusCode={StatusCode} elapsedMs={ElapsedMilliseconds} correlationId={CorrelationId}",
            context.Request.Method,
            context.Request.Path,
            context.Response.StatusCode,
            TimeProvider.System.GetElapsedTime(startedAt).TotalMilliseconds,
            correlationId);
    }
}
