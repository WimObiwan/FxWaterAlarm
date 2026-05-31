using System.Reflection;
using Core.Configuration;
using Core.Entities;
using Core.Repositories;
using Microsoft.Extensions.Options;
using Vibrant.InfluxDB.Client;
using Xunit;

namespace CoreTests.Repositories;

internal class TestableMeasurementRepositoryBase : MeasurementRepositoryBase<RecordLevel, AggregatedRecordLevel, MeasurementLevel, AggregatedMeasurementLevel>
{
    public TestableMeasurementRepositoryBase(MeasurementInfluxOptions options)
        : base(Options.Create(options))
    {
    }

    public TestableMeasurementRepositoryBase()
        : this(new MeasurementInfluxOptions
        {
            Endpoint = new Uri("http://localhost:8086"),
            Username = string.Empty,
            Password = string.Empty
        })
    {
    }

    protected override string GetTableName() => "test_table";

    protected override MeasurementLevel ReturnMeasurement(InfluxSeries<RecordLevel> series, RecordLevel record)
    {
        return new MeasurementLevel
        {
            DevEui = record.DevEui,
            Timestamp = record.Timestamp,
            DistanceMm = (int)record.Distance,
            BatV = record.BatV,
            RssiDbm = record.Rssi
        };
    }

    protected override AggregatedMeasurementLevel ReturnAggregatedMeasurement(InfluxSeries<AggregatedRecordLevel> series, AggregatedRecordLevel record)
    {
        return new AggregatedMeasurementLevel
        {
            DevEui = record.DevEui,
            Timestamp = record.Timestamp,
            BatV = record.BatV,
            RssiDbm = record.Rssi
        };
    }

    public (string filterText, object parameters) TestGetFilter(string devEui, DateTime? from, DateTime? till)
    {
        var method = typeof(MeasurementRepositoryBase<RecordLevel, AggregatedRecordLevel, MeasurementLevel, AggregatedMeasurementLevel>)
            .GetMethod("GetFilter", BindingFlags.NonPublic | BindingFlags.Instance);

        var result = method!.Invoke(this, new object?[] { devEui, from, till });
        return ((string, object))result!;
    }
}

public class MeasurementRepositoryBaseTest
{
    [Fact]
    public async Task GetLast_WhenInfluxIsUnavailable_Throws()
    {
        var repo = new TestableMeasurementRepositoryBase();

        // We only assert that the path executes and bubbles the connection error.
        await Assert.ThrowsAnyAsync<Exception>(() => repo.GetLast("DEV001", CancellationToken.None));
    }

    [Fact]
    public async Task Write_WhenInfluxIsUnavailable_Throws()
    {
        var repo = new TestableMeasurementRepositoryBase();
        var record = new RecordLevel
        {
            DevEui = "DEV001",
            Timestamp = DateTime.UtcNow,
            BatV = 3.3,
            Distance = 1000,
            Rssi = -80
        };

        // We only assert that the path executes and bubbles the connection error.
        await Assert.ThrowsAnyAsync<Exception>(() => repo.Write(record, CancellationToken.None));
    }

    [Fact]
    public void GetFilter_DevEuiOnly_ReturnsWhereClause()
    {
        var repo = new TestableMeasurementRepositoryBase();
        var (filterText, parameters) = repo.TestGetFilter("DEV001", null, null);

        Assert.Equal(" WHERE DevEUI = $devEui", filterText);
        var dict = Assert.IsType<Dictionary<string, object>>(parameters);
        Assert.Single(dict);
        Assert.Equal("DEV001", dict["devEui"]);
    }

    [Fact]
    public void GetFilter_WithFromAndTill_IncludesBothClauses()
    {
        var repo = new TestableMeasurementRepositoryBase();
        var from = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var till = new DateTime(2026, 1, 2, 12, 0, 0, DateTimeKind.Utc);
        var (filterText, parameters) = repo.TestGetFilter("DEV001", from, till);

        Assert.Equal(" WHERE DevEUI = $devEui AND time >= $from AND time < $till", filterText);
        var dict = Assert.IsType<Dictionary<string, object>>(parameters);
        Assert.Equal(3, dict.Count);
        Assert.Equal("DEV001", dict["devEui"]);
        Assert.Equal(from, dict["from"]);
        Assert.Equal(till, dict["till"]);
    }
}
