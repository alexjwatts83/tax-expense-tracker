using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NLog.Web;
using TaxExpenseTracker.Application.Common;
using TaxExpenseTracker.Application.Banks;
using TaxExpenseTracker.Application.DataTransfer;
using TaxExpenseTracker.Application.Expenses;
using TaxExpenseTracker.Application.Leave;
using TaxExpenseTracker.Application.PublicHolidays;
using TaxExpenseTracker.Application.Tags;
using TaxExpenseTracker.Application.Trackers;
using TaxExpenseTracker.Application.WorkLocation;
using TaxExpenseTracker.Infrastructure.Data;
using TaxExpenseTracker.Api.Middleware;
using TaxExpenseTracker.Api.Extensions;
using TaxExpenseTracker.Api.Options;

var builder = WebApplication.CreateBuilder(args);
const string AppCorsPolicy = "AppCorsPolicy";

// Add services to the container.

builder.Services.AddControllers().ConfigureApiBehaviorOptions(options =>
{
    options.SuppressMapClientErrors = true;
    options.InvalidModelStateResponseFactory = actionContext =>
    {
        var errors = actionContext.ModelState
            .Where(entry => entry.Value?.Errors.Count > 0)
            .ToDictionary(
                entry => entry.Key,
                entry => entry.Value!.Errors
                    .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage)
                        ? "The request is invalid."
                        : error.ErrorMessage)
                    .ToArray());
        var problem = ApiProblemDetails.CreateValidation(actionContext.HttpContext, errors);
        return new BadRequestObjectResult(problem)
        {
            ContentTypes = { "application/problem+json" },
        };
    };
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services
    .AddOptions<EntraOptions>()
    .BindConfiguration(EntraOptions.SectionName)
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<EntraOptions>, EntraOptionsValidator>();
var authenticationEnabled = builder.Services.AddEntraAuthentication(builder.Configuration);
var developmentAuthenticationEnabled = builder.Services.AddDevelopmentAuthentication(
    builder.Configuration,
    builder.Environment);
authenticationEnabled |= developmentAuthenticationEnabled;
builder.Services.AddApplicationAuthorization(builder.Configuration);

var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>()?
    .Select(origin => origin.Trim())
    .Where(origin => !string.IsNullOrWhiteSpace(origin))
    .Distinct(StringComparer.OrdinalIgnoreCase)
    .ToArray()
    ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy(AppCorsPolicy, policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy
                .WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
    });
});

builder.Logging.ClearProviders();
builder.Host.UseNLog();

var defaultConnection = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Missing connection string 'DefaultConnection'. Configure ConnectionStrings__DefaultConnection for cloud environments.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(defaultConnection, sqlite =>
        sqlite.MigrationsAssembly("TaxExpenseTracker.Infrastructure")));

builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddScoped<ITrackerRepository, EfTrackerRepository>();
builder.Services.AddScoped<ITrackerService, TrackerService>();
builder.Services.AddScoped<ITagRepository, EfTagRepository>();
builder.Services.AddScoped<ITagService, TagService>();
builder.Services.AddScoped<IBankRepository, EfBankRepository>();
builder.Services.AddScoped<IBankService, BankService>();
builder.Services.AddScoped<IExpenseRepository, EfExpenseRepository>();
builder.Services.AddScoped<IExpenseService, ExpenseService>();
builder.Services.AddScoped<IWorkLocationRepository, EfWorkLocationRepository>();
builder.Services.AddScoped<IWorkLocationService, WorkLocationService>();
builder.Services.AddScoped<ILeaveRepository, EfLeaveRepository>();
builder.Services.AddScoped<ILeaveService, LeaveService>();
builder.Services.AddScoped<IPublicHolidayRepository, EfPublicHolidayRepository>();
builder.Services.AddScoped<IPublicHolidayService, PublicHolidayService>();
builder.Services.AddDataTransferServices();

var app = builder.Build();

if (!authenticationEnabled)
    app.Logger.LogWarning("Authentication is disabled. Set Entra:Enabled to true before exposing the API publicly.");
else if (developmentAuthenticationEnabled)
    app.Logger.LogWarning("Development test authentication is enabled. Do not expose this Development environment publicly.");

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ApiExceptionHandlingMiddleware>();
app.UseMiddleware<DataTransferRequestSizeMiddleware>();
app.UseStatusCodePages(async statusCodeContext =>
{
    var context = statusCodeContext.HttpContext;
    if (context.Response.StatusCode == StatusCodes.Status404NotFound)
    {
        await ApiProblemDetails.WriteAsync(
            context,
            StatusCodes.Status404NotFound,
            "not_found",
            "Resource not found",
            "The requested resource was not found.");
    }
});

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();
}

app.UseHttpsRedirection();

app.UseCors(AppCorsPolicy);

if (authenticationEnabled)
    app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;
