namespace TaxExpenseTracker.Api.Middleware;

public sealed class DataTransferRequestSizeMiddleware
{
    // DEC-HARD-005: 12 MiB import request ceiling matching the 10 MiB browser file limit.
    public const long MaxRequestBytes = 12L * 1024 * 1024;

    private const string ImportPathPrefix = "/api/data-transfer/import";

    private readonly RequestDelegate _next;

    public DataTransferRequestSizeMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (HttpMethods.IsPost(context.Request.Method)
            && context.Request.Path.StartsWithSegments(ImportPathPrefix, StringComparison.OrdinalIgnoreCase)
            && context.Request.ContentLength > MaxRequestBytes)
        {
            await ApiProblemDetails.WriteAsync(
                context,
                StatusCodes.Status413PayloadTooLarge,
                "payload_too_large",
                "Payload too large",
                "The import request exceeds the maximum allowed size of 12 MiB.");
            return;
        }

        await _next(context);
    }
}
