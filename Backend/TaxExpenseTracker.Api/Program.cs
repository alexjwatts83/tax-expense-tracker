using Microsoft.EntityFrameworkCore;
using NLog.Web;
using TaxExpenseTracker.Application.Banks;
using TaxExpenseTracker.Application.Expenses;
using TaxExpenseTracker.Application.Tags;
using TaxExpenseTracker.Application.Trackers;
using TaxExpenseTracker.Infrastructure.Data;
using TaxExpenseTracker.Api.Middleware;

var builder = WebApplication.CreateBuilder(args);
const string AppCorsPolicy = "AppCorsPolicy";

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

builder.Services.AddScoped<ITrackerRepository, EfTrackerRepository>();
builder.Services.AddScoped<ITrackerService, TrackerService>();
builder.Services.AddScoped<ITagRepository, EfTagRepository>();
builder.Services.AddScoped<ITagService, TagService>();
builder.Services.AddScoped<IBankRepository, EfBankRepository>();
builder.Services.AddScoped<IBankService, BankService>();
builder.Services.AddScoped<IExpenseRepository, EfExpenseRepository>();
builder.Services.AddScoped<IExpenseService, ExpenseService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ApiExceptionHandlingMiddleware>();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();
}

app.UseHttpsRedirection();

app.UseCors(AppCorsPolicy);

app.UseAuthorization();

app.MapControllers();

app.Run();
