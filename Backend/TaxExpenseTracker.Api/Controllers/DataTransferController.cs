using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using TaxExpenseTracker.Application.DataTransfer;

namespace TaxExpenseTracker.Api.Controllers;

[ApiController]
[Route("api/data-transfer")]
public class DataTransferController(
    IDataTransferService dataTransferService,
    ILogger<DataTransferController> logger,
    IHostEnvironment hostEnvironment,
    IConfiguration configuration) : ControllerBase
{
    private static readonly TimeSpan ImportTimeout = TimeSpan.FromMinutes(5);
    private static readonly JsonSerializerOptions StreamSerializerOptions = new(JsonSerializerDefaults.Web);
    private const string EnableEndpointsConfigKey = "Features:EnableDataTransferEndpoints";

    [HttpGet("export")]
    public async Task<IActionResult> ExportReferenceData(
        bool includeSoftDeleted = true,
        CancellationToken cancellationToken = default)
    {
        var disabledResult = EnsureEnabled();
        if (disabledResult is not null)
        {
            return disabledResult;
        }

        logger.LogInformation(
            "Data transfer export started. Endpoint=reference includeSoftDeleted={IncludeSoftDeleted} correlationId={CorrelationId}",
            includeSoftDeleted,
            HttpContext.TraceIdentifier);

        var payload = await dataTransferService.ExportReferenceDataAsync(includeSoftDeleted, cancellationToken);

        logger.LogInformation(
            "Data transfer export completed. Endpoint=reference trackers={Trackers} tags={Tags} banks={Banks} holidays={Holidays} correlationId={CorrelationId}",
            payload.Data.Trackers.Count,
            payload.Data.Tags.Count,
            payload.Data.Banks.Count,
            payload.Data.PublicHolidays.Count,
            HttpContext.TraceIdentifier);

        return await StreamJsonAsync(payload, cancellationToken);
    }

    [HttpGet("export/{entityName}")]
    public async Task<IActionResult> ExportEntity(
        string entityName,
        bool includeSoftDeleted = true,
        CancellationToken cancellationToken = default)
    {
        var disabledResult = EnsureEnabled();
        if (disabledResult is not null)
        {
            return disabledResult;
        }

        var normalized = NormalizeEntityName(entityName);
        if (normalized is null)
        {
            return NotFound();
        }

        logger.LogInformation(
            "Data transfer export started. Endpoint=entity entity={Entity} includeSoftDeleted={IncludeSoftDeleted} correlationId={CorrelationId}",
            normalized,
            includeSoftDeleted,
            HttpContext.TraceIdentifier);

        var reference = await dataTransferService.ExportReferenceDataAsync(includeSoftDeleted, cancellationToken);

        object? payload = normalized switch
        {
            "trackers" => reference.Data.Trackers,
            "tags" => reference.Data.Tags,
            "banks" => reference.Data.Banks,
            "public-holidays" => reference.Data.PublicHolidays,
            "expenses" => await dataTransferService.ExportExpensesAsync(includeSoftDeleted, cancellationToken),
            "expense-tags" => (await dataTransferService.ExportExpensesAsync(includeSoftDeleted, cancellationToken)).ExpenseTags ?? [],
            "work-locations" => await dataTransferService.ExportWorkLocationsAsync(includeSoftDeleted, cancellationToken),
            "leave" => await dataTransferService.ExportLeaveAsync(includeSoftDeleted, cancellationToken),
            _ => null,
        };

        if (payload is null)
        {
            return NotFound();
        }

        logger.LogInformation(
            "Data transfer export completed. Endpoint=entity entity={Entity} correlationId={CorrelationId}",
            normalized,
            HttpContext.TraceIdentifier);

        return await StreamJsonAsync(payload, cancellationToken);
    }

    [HttpPost("import")]
    [RequestSizeLimit(100 * 1024 * 1024)]
    public async Task<IActionResult> ImportReferenceData(
        [FromBody] ReferenceDataImportPayloadDto payload,
        DataTransferImportMode mode = DataTransferImportMode.Upsert,
        bool dryRun = false,
        bool allowDeletes = false,
        CancellationToken cancellationToken = default)
    {
        var disabledResult = EnsureEnabled();
        if (disabledResult is not null)
        {
            return disabledResult;
        }

        logger.LogInformation(
            "Data transfer import started. Endpoint=reference mode={Mode} dryRun={DryRun} allowDeletes={AllowDeletes} correlationId={CorrelationId}",
            mode,
            dryRun,
            allowDeletes,
            HttpContext.TraceIdentifier);

        return await ExecuteImportWithTimeoutAsync(
            token => dataTransferService.ImportReferenceDataAsync(
                payload,
                new DataTransferImportOptions(mode, dryRun, allowDeletes),
                token),
            cancellationToken);
    }

    [HttpPost("import/{entityName}")]
    [RequestSizeLimit(100 * 1024 * 1024)]
    public async Task<IActionResult> ImportEntity(
        string entityName,
        [FromBody] JsonElement payload,
        DataTransferImportMode mode = DataTransferImportMode.Upsert,
        bool dryRun = false,
        bool allowDeletes = false,
        CancellationToken cancellationToken = default)
    {
        var disabledResult = EnsureEnabled();
        if (disabledResult is not null)
        {
            return disabledResult;
        }

        var normalized = NormalizeEntityName(entityName);
        if (normalized is null)
        {
            return NotFound();
        }

        logger.LogInformation(
            "Data transfer import started. Endpoint=entity entity={Entity} mode={Mode} dryRun={DryRun} allowDeletes={AllowDeletes} correlationId={CorrelationId}",
            normalized,
            mode,
            dryRun,
            allowDeletes,
            HttpContext.TraceIdentifier);

        var options = new DataTransferImportOptions(mode, dryRun, allowDeletes);
        var serializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        return normalized switch
        {
            "trackers" => await ExecuteImportWithTimeoutAsync(
                token => dataTransferService.ImportReferenceDataAsync(
                    new ReferenceDataImportPayloadDto(new ReferenceDataImportDataDto(
                        Trackers: DeserializeRequired<IReadOnlyList<ReferenceTrackerImportItemDto>>(payload, serializerOptions),
                        Tags: null,
                        Banks: null,
                        PublicHolidays: null)),
                    options,
                    token),
                cancellationToken),
            "tags" => await ExecuteImportWithTimeoutAsync(
                token => dataTransferService.ImportReferenceDataAsync(
                    new ReferenceDataImportPayloadDto(new ReferenceDataImportDataDto(
                        Trackers: null,
                        Tags: DeserializeRequired<IReadOnlyList<ReferenceTagImportItemDto>>(payload, serializerOptions),
                        Banks: null,
                        PublicHolidays: null)),
                    options,
                    token),
                cancellationToken),
            "banks" => await ExecuteImportWithTimeoutAsync(
                token => dataTransferService.ImportReferenceDataAsync(
                    new ReferenceDataImportPayloadDto(new ReferenceDataImportDataDto(
                        Trackers: null,
                        Tags: null,
                        Banks: DeserializeRequired<IReadOnlyList<ReferenceBankImportItemDto>>(payload, serializerOptions),
                        PublicHolidays: null)),
                    options,
                    token),
                cancellationToken),
            "public-holidays" => await ExecuteImportWithTimeoutAsync(
                token => dataTransferService.ImportReferenceDataAsync(
                    new ReferenceDataImportPayloadDto(new ReferenceDataImportDataDto(
                        Trackers: null,
                        Tags: null,
                        Banks: null,
                        PublicHolidays: DeserializeRequired<IReadOnlyList<ReferencePublicHolidayImportItemDto>>(payload, serializerOptions))),
                    options,
                    token),
                cancellationToken),
            "expenses" => await ExecuteImportWithTimeoutAsync(
                token => dataTransferService.ImportExpensesAsync(
                    DeserializeRequired<ExpenseImportPayloadDto>(payload, serializerOptions),
                    options,
                    token),
                cancellationToken),
            "expense-tags" => await ExecuteImportWithTimeoutAsync(
                token => dataTransferService.ImportExpensesAsync(
                    new ExpenseImportPayloadDto(
                        Expenses: null,
                        ExpenseTags: DeserializeRequired<IReadOnlyList<ExpenseTagImportItemDto>>(payload, serializerOptions)),
                    options,
                    token),
                cancellationToken),
            "work-locations" => await ExecuteImportWithTimeoutAsync(
                token => dataTransferService.ImportWorkLocationsAsync(
                    new WorkLocationImportPayloadDto(
                        DeserializeRequired<IReadOnlyList<WorkLocationEntryImportItemDto>>(payload, serializerOptions)),
                    options,
                    token),
                cancellationToken),
            "leave" => await ExecuteImportWithTimeoutAsync(
                token => dataTransferService.ImportLeaveAsync(
                    new LeaveImportPayloadDto(
                        DeserializeRequired<IReadOnlyList<LeaveEntryImportItemDto>>(payload, serializerOptions)),
                    options,
                    token),
                cancellationToken),
            _ => NotFound(),
        };
    }

    [HttpPost("import/expenses")]
    [RequestSizeLimit(100 * 1024 * 1024)]
    public async Task<IActionResult> ImportExpenses(
        [FromBody] ExpenseImportPayloadDto payload,
        DataTransferImportMode mode = DataTransferImportMode.Upsert,
        bool dryRun = false,
        bool allowDeletes = false,
        CancellationToken cancellationToken = default)
    {
        var disabledResult = EnsureEnabled();
        if (disabledResult is not null)
        {
            return disabledResult;
        }

        logger.LogInformation(
            "Data transfer import started. Endpoint=expenses mode={Mode} dryRun={DryRun} allowDeletes={AllowDeletes} correlationId={CorrelationId}",
            mode,
            dryRun,
            allowDeletes,
            HttpContext.TraceIdentifier);

        return await ExecuteImportWithTimeoutAsync(
            token => dataTransferService.ImportExpensesAsync(
                payload,
                new DataTransferImportOptions(mode, dryRun, allowDeletes),
                token),
            cancellationToken);
    }

    [HttpPost("import/work-locations")]
    [RequestSizeLimit(100 * 1024 * 1024)]
    public async Task<IActionResult> ImportWorkLocations(
        [FromBody] WorkLocationImportPayloadDto payload,
        DataTransferImportMode mode = DataTransferImportMode.Upsert,
        bool dryRun = false,
        bool allowDeletes = false,
        CancellationToken cancellationToken = default)
    {
        var disabledResult = EnsureEnabled();
        if (disabledResult is not null)
        {
            return disabledResult;
        }

        logger.LogInformation(
            "Data transfer import started. Endpoint=work-locations mode={Mode} dryRun={DryRun} allowDeletes={AllowDeletes} correlationId={CorrelationId}",
            mode,
            dryRun,
            allowDeletes,
            HttpContext.TraceIdentifier);

        return await ExecuteImportWithTimeoutAsync(
            token => dataTransferService.ImportWorkLocationsAsync(
                payload,
                new DataTransferImportOptions(mode, dryRun, allowDeletes),
                token),
            cancellationToken);
    }

    [HttpPost("import/leave")]
    [RequestSizeLimit(100 * 1024 * 1024)]
    public async Task<IActionResult> ImportLeave(
        [FromBody] LeaveImportPayloadDto payload,
        DataTransferImportMode mode = DataTransferImportMode.Upsert,
        bool dryRun = false,
        bool allowDeletes = false,
        CancellationToken cancellationToken = default)
    {
        var disabledResult = EnsureEnabled();
        if (disabledResult is not null)
        {
            return disabledResult;
        }

        logger.LogInformation(
            "Data transfer import started. Endpoint=leave mode={Mode} dryRun={DryRun} allowDeletes={AllowDeletes} correlationId={CorrelationId}",
            mode,
            dryRun,
            allowDeletes,
            HttpContext.TraceIdentifier);

        return await ExecuteImportWithTimeoutAsync(
            token => dataTransferService.ImportLeaveAsync(
                payload,
                new DataTransferImportOptions(mode, dryRun, allowDeletes),
                token),
            cancellationToken);
    }

    private static string? NormalizeEntityName(string entityName)
    {
        if (string.IsNullOrWhiteSpace(entityName))
        {
            return null;
        }

        var normalized = entityName.Trim().ToLowerInvariant();
        return normalized switch
        {
            "trackers" => normalized,
            "tags" => normalized,
            "banks" => normalized,
            "expenses" => normalized,
            "expense-tags" => normalized,
            "work-locations" => normalized,
            "leave" => normalized,
            "public-holidays" => normalized,
            _ => null,
        };
    }

    private static T DeserializeRequired<T>(JsonElement payload, JsonSerializerOptions serializerOptions)
    {
        var value = payload.Deserialize<T>(serializerOptions);
        if (value is null)
        {
            throw new ArgumentException($"Request body could not be deserialized to {typeof(T).Name}.");
        }

        return value;
    }

    private async Task<IActionResult> StreamJsonAsync(object payload, CancellationToken cancellationToken)
    {
        Response.ContentType = "application/json; charset=utf-8";
        await JsonSerializer.SerializeAsync(Response.Body, payload, StreamSerializerOptions, cancellationToken);
        return new EmptyResult();
    }

    private async Task<IActionResult> ExecuteImportWithTimeoutAsync(
        Func<CancellationToken, Task<DataTransferImportResultDto>> action,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(action);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(ImportTimeout);

        try
        {
            var result = await action(timeoutCts.Token);
            var enriched = result with { CorrelationId = HttpContext.TraceIdentifier };

            logger.LogInformation(
                "Data transfer import completed. mode={Mode} dryRun={DryRun} entities={EntityCount} correlationId={CorrelationId}",
                enriched.Mode,
                enriched.DryRun,
                enriched.Results.Count,
                HttpContext.TraceIdentifier);

            return Ok(enriched);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(
                "Data transfer import timed out after {TimeoutMinutes} minutes. correlationId={CorrelationId}",
                ImportTimeout.TotalMinutes,
                HttpContext.TraceIdentifier);

            return StatusCode(StatusCodes.Status408RequestTimeout, new
            {
                message = $"Import operation exceeded timeout of {ImportTimeout.TotalMinutes:0} minutes.",
                correlationId = HttpContext.TraceIdentifier,
            });
        }
    }

    private IActionResult? EnsureEnabled()
    {
        if (!hostEnvironment.IsProduction())
        {
            return null;
        }

        var enabled = configuration.GetValue<bool>(EnableEndpointsConfigKey);
        if (enabled)
        {
            return null;
        }

        logger.LogWarning(
            "Data transfer endpoint blocked in production because feature flag {ConfigKey} is false. correlationId={CorrelationId}",
            EnableEndpointsConfigKey,
            HttpContext.TraceIdentifier);

        return StatusCode(StatusCodes.Status403Forbidden, new
        {
            message = "Data transfer endpoints are disabled in production.",
            correlationId = HttpContext.TraceIdentifier,
        });
    }
}
