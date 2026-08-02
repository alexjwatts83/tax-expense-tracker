using System.Net;
using System.Net.Http.Json;
using TaxExpenseTracker.Api.Models;

namespace TaxExpenseTracker.Tests.Integration;

public class ApiHostingTests
{
    [Fact]
    public async Task Factories_UseIsolatedDatabases()
    {
        const string trackerName = "Integration isolation tracker";

        using (var firstFactory = new TaxExpenseTrackerApiFactory())
        {
            using var firstClient = firstFactory.CreateClient();
            var createResponse = await firstClient.PostAsJsonAsync(
                "/api/trackers",
                new CreateTrackerDto { Name = trackerName });

            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
            var firstTrackers = await firstClient.GetFromJsonAsync<TrackerDto[]>("/api/trackers");
            Assert.Contains(firstTrackers!, tracker => tracker.Name == trackerName);
        }

        using var secondFactory = new TaxExpenseTrackerApiFactory();
        using var secondClient = secondFactory.CreateClient();
        var secondTrackers = await secondClient.GetFromJsonAsync<TrackerDto[]>("/api/trackers");

        Assert.DoesNotContain(secondTrackers!, tracker => tracker.Name == trackerName);
    }
}