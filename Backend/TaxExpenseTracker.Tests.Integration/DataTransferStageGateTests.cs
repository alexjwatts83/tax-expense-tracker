using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace TaxExpenseTracker.Tests.Integration;

public class DataTransferStageGateTests
{
    [Fact]
    public async Task ReferenceImport_WithMalformedJson_ReturnsValidationProblem()
    {
        using var factory = new TaxExpenseTrackerApiFactory(dataTransferEnabled: true);
        using var client = factory.CreateClient();
        using var content = new StringContent("{\"data\":", Encoding.UTF8, "application/json");

        using var response = await client.PostAsync("/api/data-transfer/import", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        using var problem = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.Equal("validation_error", problem.RootElement.GetProperty("code").GetString());
        Assert.False(string.IsNullOrWhiteSpace(problem.RootElement.GetProperty("correlationId").GetString()));
    }

    [Fact]
    public async Task ReferenceImport_DryRun_ReportsCreationWithoutPersistingIt()
    {
        using var factory = new TaxExpenseTrackerApiFactory(dataTransferEnabled: true);
        using var client = factory.CreateClient();
        var trackerId = Guid.NewGuid();
        var payload = new
        {
            data = new
            {
                trackers = new[] { new { id = trackerId, name = "Dry-run tracker" } },
            },
        };

        using var importResponse = await client.PostAsJsonAsync(
            "/api/data-transfer/import?dryRun=true",
            payload);

        importResponse.EnsureSuccessStatusCode();
        using var importResult = await JsonDocument.ParseAsync(await importResponse.Content.ReadAsStreamAsync());
        Assert.True(importResult.RootElement.GetProperty("dryRun").GetBoolean());
        Assert.Equal(
            1,
            importResult.RootElement.GetProperty("results")[0].GetProperty("createdCount").GetInt32());

        using var export = await client.GetFromJsonAsync<JsonDocument>("/api/data-transfer/export/trackers");
        Assert.DoesNotContain(
            export!.RootElement.EnumerateArray(),
            tracker => tracker.GetProperty("id").GetGuid() == trackerId);
    }
}