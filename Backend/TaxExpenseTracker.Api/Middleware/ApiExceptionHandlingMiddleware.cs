using TaxExpenseTracker.Application.Common;

namespace TaxExpenseTracker.Api.Middleware;

public sealed class ApiExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiExceptionHandlingMiddleware> _logger;

    public ApiExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ApiExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ApplicationValidationException ex)
        {
            await ApiProblemDetails.WriteAsync(context, StatusCodes.Status400BadRequest, "validation_error", "Validation error", ex.Message);
        }
        catch (MissingReferenceException ex)
        {
            await ApiProblemDetails.WriteAsync(context, StatusCodes.Status400BadRequest, "invalid_reference", "Invalid reference", ex.Message);
        }
        catch (ResourceNotFoundException ex)
        {
            await ApiProblemDetails.WriteAsync(context, StatusCodes.Status404NotFound, "not_found", "Resource not found", ex.Message);
        }
        catch (ApplicationConflictException ex)
        {
            await ApiProblemDetails.WriteAsync(context, StatusCodes.Status409Conflict, "conflict", "Conflict", ex.Message);
        }
        catch (ArgumentException ex)
        {
            await ApiProblemDetails.WriteAsync(context, StatusCodes.Status400BadRequest, "validation_error", "Validation error", ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            await ApiProblemDetails.WriteAsync(context, StatusCodes.Status400BadRequest, "validation_error", "Validation error", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unexpected API exception. method={Method} path={Path} correlationId={CorrelationId}",
                context.Request.Method,
                context.Request.Path,
                context.TraceIdentifier);

            await ApiProblemDetails.WriteAsync(
                context,
                StatusCodes.Status500InternalServerError,
                "server_error",
                "Server error",
                "An unexpected server error occurred.");
        }
    }

}
