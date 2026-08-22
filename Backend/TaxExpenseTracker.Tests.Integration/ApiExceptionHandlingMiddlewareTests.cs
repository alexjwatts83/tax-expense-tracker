using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using TaxExpenseTracker.Api.Middleware;
using TaxExpenseTracker.Application.Common;

namespace TaxExpenseTracker.Tests.Integration;

public class ApiExceptionHandlingMiddlewareTests
{
    public static TheoryData<Exception, int, string, string> TypedFailures => new()
    {
        { new ApplicationValidationException("Invalid expense."), StatusCodes.Status400BadRequest, "validation_error", "Validation error" },
        { new MissingReferenceException("Bank", Guid.NewGuid()), StatusCodes.Status400BadRequest, "invalid_reference", "Invalid reference" },
        { new ResourceNotFoundException("Expense", Guid.NewGuid()), StatusCodes.Status404NotFound, "not_found", "Resource not found" },
        { new ApplicationConflictException("Expense already exists."), StatusCodes.Status409Conflict, "conflict", "Conflict" },
    };

    [Fact]
    public async Task ControllerNotFound_ReturnsAgreedProblemContract()
    {
        using var factory = new TaxExpenseTrackerApiFactory();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/trackers/{Guid.NewGuid()}");
        request.Headers.Add(CorrelationIdMiddleware.HeaderName, "not-found-correlation");

        using var response = await client.SendAsync(request);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("not_found", problem.RootElement.GetProperty("code").GetString());
        Assert.Equal("not-found-correlation", problem.RootElement.GetProperty("correlationId").GetString());
        Assert.Equal(request.RequestUri!.AbsolutePath, problem.RootElement.GetProperty("instance").GetString());
    }

    [Fact]
    public async Task InvalidRequestBody_ReturnsValidationProblemWithFieldErrors()
    {
        using var factory = new TaxExpenseTrackerApiFactory();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/trackers")
        {
            Content = new StringContent("{\"name\":", Encoding.UTF8, "application/json"),
        };
        request.Headers.Add(CorrelationIdMiddleware.HeaderName, "validation-correlation");

        using var response = await client.SendAsync(request);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("validation_error", problem.RootElement.GetProperty("code").GetString());
        Assert.Equal("validation-correlation", problem.RootElement.GetProperty("correlationId").GetString());
        Assert.Equal(JsonValueKind.Object, problem.RootElement.GetProperty("errors").ValueKind);
    }

    [Theory]
    [MemberData(nameof(TypedFailures))]
    public async Task TypedApplicationException_ReturnsAgreedProblemContract(
        Exception exception,
        int expectedStatus,
        string expectedCode,
        string expectedTitle)
    {
        var middleware = new ApiExceptionHandlingMiddleware(
            _ => throw exception,
            new CapturingLogger<ApiExceptionHandlingMiddleware>());
        var context = CreateContext();

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        using var problem = await JsonDocument.ParseAsync(context.Response.Body);
        var root = problem.RootElement;
        Assert.Equal(expectedStatus, context.Response.StatusCode);
        Assert.Equal("application/problem+json", context.Response.ContentType);
        Assert.Equal(expectedStatus, root.GetProperty("status").GetInt32());
        Assert.Equal(expectedTitle, root.GetProperty("title").GetString());
        Assert.Equal(exception.Message, root.GetProperty("detail").GetString());
        Assert.Equal("/api/test", root.GetProperty("instance").GetString());
        Assert.Equal(expectedCode, root.GetProperty("code").GetString());
        Assert.Equal($"https://tax-expense-tracker.dev/problems/{expectedCode}", root.GetProperty("type").GetString());
        Assert.Equal("middleware-correlation-id", root.GetProperty("correlationId").GetString());
    }

    [Fact]
    public async Task UnexpectedException_IsLoggedWithRequestContext_AndReturnsSafeProblem()
    {
        var expectedException = new Exception("Sensitive failure detail");
        var logger = new CapturingLogger<ApiExceptionHandlingMiddleware>();
        var middleware = new ApiExceptionHandlingMiddleware(
            _ => throw expectedException,
            logger);
        var context = CreateContext();
        context.Request.Method = HttpMethods.Post;
        context.Request.Path = "/api/expenses";

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
        Assert.Equal("server_error", problem.RootElement.GetProperty("code").GetString());
        Assert.Equal("middleware-correlation-id", problem.RootElement.GetProperty("correlationId").GetString());
        Assert.DoesNotContain("Sensitive failure detail", problem.RootElement.GetRawText());
    }

    private static DefaultHttpContext CreateContext()
    {
        var context = new DefaultHttpContext
        {
            TraceIdentifier = "middleware-correlation-id",
        };
        context.Request.Path = "/api/test";
        context.Response.Body = new MemoryStream();
        return context;
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