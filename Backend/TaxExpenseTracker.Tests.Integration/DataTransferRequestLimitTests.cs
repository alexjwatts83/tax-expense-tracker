using System.Net;
using System.Text;

namespace TaxExpenseTracker.Tests.Integration;

public class DataTransferRequestLimitTests
{
    [Fact]
    public async Task ReferenceImport_OverTwelveMebibytes_IsRejected()
    {
        using var factory = new TaxExpenseTrackerApiFactory(dataTransferEnabled: true);
        using var client = factory.CreateClient();

        var oversizedName = new string('a', 13 * 1024 * 1024);
        var json = $"{{\"data\":{{\"trackers\":[{{\"id\":\"{Guid.NewGuid()}\",\"name\":\"{oversizedName}\"}}]}}}}";
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var response = await client.PostAsync("/api/data-transfer/import", content);

        Assert.Equal(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
    }

    [Fact]
    public async Task ReferenceImport_WithinLimit_IsNotRejectedForSize()
    {
        using var factory = new TaxExpenseTrackerApiFactory(dataTransferEnabled: true);
        using var client = factory.CreateClient();

        var json = $"{{\"data\":{{\"trackers\":[{{\"id\":\"{Guid.NewGuid()}\",\"name\":\"Small\"}}]}}}}";
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var response = await client.PostAsync("/api/data-transfer/import", content);

        Assert.NotEqual(HttpStatusCode.RequestEntityTooLarge, response.StatusCode);
    }
}
