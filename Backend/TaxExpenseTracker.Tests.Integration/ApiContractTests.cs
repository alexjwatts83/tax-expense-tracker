using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TaxExpenseTracker.Api.Middleware;
using TaxExpenseTracker.Api.Models;
using TaxExpenseTracker.Domain.Entities;

namespace TaxExpenseTracker.Tests.Integration;

public class ApiContractTests
{
    [Fact]
    public async Task TrackerRoute_ReturnsCamelCaseJson()
    {
        using var factory = new TaxExpenseTrackerApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/trackers");
        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tracker = document.RootElement.EnumerateArray().First();
        Assert.True(tracker.TryGetProperty("id", out _));
        Assert.True(tracker.TryGetProperty("name", out _));
        Assert.True(tracker.TryGetProperty("description", out _));
        Assert.True(tracker.TryGetProperty("createdAt", out _));
        Assert.False(tracker.TryGetProperty("CreatedAt", out _));
    }

    [Fact]
    public async Task RequestCorrelationId_IsReturnedUnchanged()
    {
        const string correlationId = "contract-test-correlation";
        using var factory = new TaxExpenseTrackerApiFactory();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/trackers");
        request.Headers.Add(CorrelationIdMiddleware.HeaderName, correlationId);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(
            correlationId,
            Assert.Single(response.Headers.GetValues(CorrelationIdMiddleware.HeaderName)));
    }

    [Fact]
    public async Task WorkLocationBatch_ReturnsMixedItemResults()
    {
        using var factory = new TaxExpenseTrackerApiFactory();
        using var client = factory.CreateClient();
        var workDate = new DateTime(2030, 2, 11);
        var request = new CreateWorkLocationBatchDto
        {
            Items =
            [
                new CreateWorkLocationDto
                {
                    WorkDate = workDate,
                    WorkLocation = WorkLocationType.Wfh,
                    EntryType = DayEntryType.FullDay,
                },
                new CreateWorkLocationDto
                {
                    WorkDate = workDate,
                    WorkLocation = WorkLocationType.Office,
                    EntryType = DayEntryType.HalfDay,
                },
            ],
        };

        using var response = await client.PostAsJsonAsync("/api/work-locations/batch", request);
        var result = await response.Content.ReadFromJsonAsync<WorkLocationBatchResultDto>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalRequested);
        Assert.Equal(1, result.CreatedCount);
        Assert.Equal(1, result.SkippedCount);
        Assert.Equal(0, result.FailedCount);
        Assert.Collection(
            result.Results,
            item => Assert.Equal("Created", item.Status),
            item => Assert.Equal("SkippedDuplicate", item.Status));
    }

    [Fact]
    public async Task DataTransferTrackerExport_ReturnsStreamedJsonArray()
    {
        using var factory = new TaxExpenseTrackerApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync("/api/data-transfer/export/trackers");
        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(JsonValueKind.Array, document.RootElement.ValueKind);
        Assert.NotEmpty(document.RootElement.EnumerateArray());
    }
}