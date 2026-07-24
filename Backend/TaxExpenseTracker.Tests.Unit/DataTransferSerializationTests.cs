using System.Text.Json;
using TaxExpenseTracker.Application.DataTransfer;
using TaxExpenseTracker.Domain.Entities;

namespace TaxExpenseTracker.Tests.Unit;

public class DataTransferSerializationTests
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public void ReferenceEnvelope_SerializesWithWebContractAndRoundTrips()
    {
        var trackerId = Guid.NewGuid();
        var envelope = new ReferenceDataExportEnvelopeDto(
            1,
            TestTime.FixedUtcNow.UtcDateTime,
            new DataTransferSourceDto("TaxExpenseTracker", "test"),
            new ReferenceDataExportDataDto(
                [new ReferenceTrackerDto(trackerId, "Work", "Work expenses", TestTime.FixedUtcNow.UtcDateTime)],
                [],
                [],
                []));

        var json = JsonSerializer.Serialize(envelope, SerializerOptions);
        var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.Equal(1, root.GetProperty("schemaVersion").GetInt32());
        Assert.Equal("TaxExpenseTracker", root.GetProperty("source").GetProperty("app").GetString());
        Assert.Equal(trackerId, root.GetProperty("data").GetProperty("trackers")[0].GetProperty("id").GetGuid());
        Assert.False(root.TryGetProperty("SchemaVersion", out _));

        var roundTrip = JsonSerializer.Deserialize<ReferenceDataExportEnvelopeDto>(json, SerializerOptions);
        Assert.NotNull(roundTrip);
        Assert.Equal(envelope.SchemaVersion, roundTrip.SchemaVersion);
        Assert.Equal(envelope.ExportedAtUtc, roundTrip.ExportedAtUtc);
        Assert.Equal(envelope.Source, roundTrip.Source);
        Assert.Equal(Assert.Single(envelope.Data.Trackers), Assert.Single(roundTrip.Data.Trackers));
        Assert.Empty(roundTrip.Data.Tags);
        Assert.Empty(roundTrip.Data.Banks);
        Assert.Empty(roundTrip.Data.PublicHolidays);
    }

    [Fact]
    public void ExpensePayload_DeserializesAndIgnoresUnknownProperties()
    {
        var expenseId = Guid.NewGuid();
        var bankId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();
        var tagId = Guid.NewGuid();
        var json = $$"""
            {
              "expenses": [{
                "id": "{{expenseId}}",
                "date": "2026-07-24T00:00:00Z",
                "description": "Laptop",
                "price": 1200.50,
                "bankId": "{{bankId}}",
                "sourceId": "{{sourceId}}",
                "isDeleted": false,
                "futureField": "ignored"
              }],
              "expenseTags": [{
                "id": "{{Guid.NewGuid()}}",
                "taxExpenseId": "{{expenseId}}",
                "tagId": "{{tagId}}"
              }],
              "futureSection": {}
            }
            """;

        var payload = JsonSerializer.Deserialize<ExpenseImportPayloadDto>(json, SerializerOptions);

        Assert.NotNull(payload);
        var expense = Assert.Single(payload.Expenses!);
        Assert.Equal(expenseId, expense.Id);
        Assert.Equal(1200.50m, expense.Price);
        Assert.Equal(DateTimeKind.Utc, expense.Date.Kind);
        Assert.Equal(tagId, Assert.Single(payload.ExpenseTags!).TagId);
    }

    [Fact]
    public void WorkLocationPayload_RoundTripsDomainEnumsAsNumbers()
    {
        var payload = new WorkLocationImportPayloadDto(
            [new WorkLocationEntryImportItemDto(
                Guid.NewGuid(),
                new DateTime(2026, 7, 24),
                DayEntryType.SpecificHours,
                4.5m,
                "Morning",
                WorkLocationType.Office,
                null,
                null,
                false)]);

        var json = JsonSerializer.Serialize(payload, SerializerOptions);
        var document = JsonDocument.Parse(json);
        var item = document.RootElement.GetProperty("workLocationEntries")[0];

        Assert.Equal((int)DayEntryType.SpecificHours, item.GetProperty("entryType").GetInt32());
        Assert.Equal((int)WorkLocationType.Office, item.GetProperty("workLocation").GetInt32());
        var roundTrip = JsonSerializer.Deserialize<WorkLocationImportPayloadDto>(json, SerializerOptions);
        Assert.NotNull(roundTrip);
        Assert.Equal(Assert.Single(payload.WorkLocationEntries!), Assert.Single(roundTrip.WorkLocationEntries!));
    }

    [Fact]
    public void LeavePayload_RoundTripsDomainEnumsAsNumbers()
    {
        var payload = new LeaveImportPayloadDto(
            [new LeaveEntryImportItemDto(
                Guid.NewGuid(),
                new DateTime(2026, 7, 24),
                DayEntryType.HalfDay,
                null,
                "Appointment",
                LeaveType.Sick,
                null,
                null,
                false)]);

        var json = JsonSerializer.Serialize(payload, SerializerOptions);
        var document = JsonDocument.Parse(json);
        var item = document.RootElement.GetProperty("leaveEntries")[0];

        Assert.Equal((int)DayEntryType.HalfDay, item.GetProperty("entryType").GetInt32());
        Assert.Equal((int)LeaveType.Sick, item.GetProperty("leaveType").GetInt32());
        var roundTrip = JsonSerializer.Deserialize<LeaveImportPayloadDto>(json, SerializerOptions);
        Assert.NotNull(roundTrip);
        Assert.Equal(Assert.Single(payload.LeaveEntries!), Assert.Single(roundTrip.LeaveEntries!));
    }
}