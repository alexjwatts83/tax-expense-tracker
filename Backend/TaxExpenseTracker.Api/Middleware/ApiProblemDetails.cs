using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace TaxExpenseTracker.Api.Middleware;

public static class ApiProblemDetails
{
    private const string ProblemTypeBase = "https://tax-expense-tracker.dev/problems/";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static ProblemDetails Create(
        HttpContext context,
        int statusCode,
        string code,
        string title,
        string detail)
    {
        var problem = new ProblemDetails
        {
            Type = ProblemTypeBase + code,
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path,
        };

        AddExtensions(problem, context, code);
        return problem;
    }

    public static ValidationProblemDetails CreateValidation(
        HttpContext context,
        IDictionary<string, string[]> errors)
    {
        var problem = new ValidationProblemDetails(errors)
        {
            Type = ProblemTypeBase + "validation_error",
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation error",
            Detail = "One or more validation errors occurred.",
            Instance = context.Request.Path,
        };

        AddExtensions(problem, context, "validation_error");
        return problem;
    }

    public static Task WriteAsync(
        HttpContext context,
        int statusCode,
        string code,
        string title,
        string detail)
    {
        context.Response.StatusCode = statusCode;
        return context.Response.WriteAsJsonAsync(
            Create(context, statusCode, code, title, detail),
            JsonOptions,
            "application/problem+json",
            context.RequestAborted);
    }

    private static void AddExtensions(ProblemDetails problem, HttpContext context, string code)
    {
        problem.Extensions["code"] = code;
        problem.Extensions["correlationId"] = context.TraceIdentifier;
    }
}