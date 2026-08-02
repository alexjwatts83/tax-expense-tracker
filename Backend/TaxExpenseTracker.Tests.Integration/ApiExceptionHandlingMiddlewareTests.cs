using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using TaxExpenseTracker.Api.Middleware;

namespace TaxExpenseTracker.Tests.Integration;

public class ApiExceptionHandlingMiddlewareTests
{
    [Fact]
    public async Task UnexpectedException_IsLoggedWithRequestContext_AndReturnsSafeProblem()
    {
        var expectedException = new Exception("Sensitive failure detail");
        var logger = new CapturingLogger<ApiExceptionHandlingMiddleware>();
        var middleware = new ApiExceptionHandlingMiddleware(
            _ => throw expectedException,
            logger);
        var context = new DefaultHttpContext
        {
            TraceIdentifier = "middleware-correlation-id",
        };
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/api/expenses";
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Same(expectedException, entry.Exception);
        Assert.Contains("POST", entry.Message);
        Assert.Contains("/api/expenses", entry.Message);
        Assert.Contains("middleware-correlation-id", entry.Message);

        context.Response.Body.Position = 0;
        using var problem = await JsonDocument.ParseAsync(context.Response.Body);
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Equal("An unexpected server error occurred.", problem.RootElement.GetProperty("detail").GetString());
        Assert.DoesNotContain("Sensitive failure detail", problem.RootElement.GetRawText());
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<LogEntry> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add(new LogEntry(logLevel, exception, formatter(state, exception)));
        }
    }

    private sealed record LogEntry(LogLevel Level, Exception? Exception, string Message);
}