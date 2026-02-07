using Core.Entities;
using Core.Repositories;
using Microsoft.Extensions.Options;
using Vibrant.InfluxDB.Client;
using Xunit;

namespace CoreTests.Repositories;

/// <summary>
/// Concrete test subclass of MeasurementRepositoryBase to test base class logic.
/// Uses RecordLevel/AggregatedRecordLevel/MeasurementLevel/AggregatedMeasurementLevel as the generic types.
/// </summary>
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
            Username = "testuser",
            Password = "testpass"
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

    /// <summary>
    /// Expose the private GetFilter method via reflection for testing.
    /// </summary>
    public (string filterText, object parameters) TestGetFilter(string devEui, DateTime? from, DateTime? till)
    {
        var method = typeof(MeasurementRepositoryBase<RecordLevel, AggregatedRecordLevel, MeasurementLevel, AggregatedMeasurementLevel>)
            .GetMethod("GetFilter", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var result = method!.Invoke(this, [devEui, from, till]);
        var tuple = ((string, object))result!;
        return tuple;
    }
}

public class MeasurementRepositoryBaseTest
{
    [Fact]
    public void Constructor_WithValidOptions_DoesNotThrow()
    {
        var options = new MeasurementInfluxOptions
        {
            Endpoint = new Uri("http://localhost:8086"),
            Username = "user",
            Password = "pass"
        };

        var repo = new TestableMeasurementRepositoryBase(options);
        Assert.NotNull(repo);
    }

    [Fact]
    public void GetFilter_DevEuiOnly_ReturnsWhereClause()
    {
        var repo = new TestableMeasurementRepositoryBase();
        var (filterText, parameters) = repo.TestGetFilter("DEV001", null, null);

        Assert.Equal(" WHERE DevEUI = $devEui", filterText);
        var dict = (Dictionary<string, object>)parameters;
        Assert.Single(dict);
        Assert.Equal("DEV001", dict["devEui"]);
    }

    [Fact]
    public void GetFilter_WithFrom_IncludesFromClause()
    {
        var repo = new TestableMeasurementRepositoryBase();
        var from = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var (filterText, parameters) = repo.TestGetFilter("DEV001", from, null);

        Assert.Equal(" WHERE DevEUI = $devEui AND time >= $from", filterText);
        var dict = (Dictionary<string, object>)parameters;
        Assert.Equal(2, dict.Count);
        Assert.Equal("DEV001", dict["devEui"]);
        Assert.Equal(from, dict["from"]);
    }

    [Fact]
    public void GetFilter_WithTill_IncludesTillClause()
    {
        var repo = new TestableMeasurementRepositoryBase();
        var till = new DateTime(2024, 12, 31, 23, 59, 59, DateTimeKind.Utc);
        var (filterText, parameters) = repo.TestGetFilter("DEV001", null, till);

        Assert.Equal(" WHERE DevEUI = $devEui AND time < $till", filterText);
        var dict = (Dictionary<string, object>)parameters;
        Assert.Equal(2, dict.Count);
        Assert.Equal("DEV001", dict["devEui"]);
        Assert.Equal(till, dict["till"]);
    }

    [Fact]
    public void GetFilter_WithFromAndTill_IncludesBothClauses()
    {
        var repo = new TestableMeasurementRepositoryBase();
        var from = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var till = new DateTime(2024, 12, 31, 23, 59, 59, DateTimeKind.Utc);
        var (filterText, parameters) = repo.TestGetFilter("DEV001", from, till);

        Assert.Equal(" WHERE DevEUI = $devEui AND time >= $from AND time < $till", filterText);
        var dict = (Dictionary<string, object>)parameters;
        Assert.Equal(3, dict.Count);
        Assert.Equal("DEV001", dict["devEui"]);
        Assert.Equal(from, dict["from"]);
        Assert.Equal(till, dict["till"]);
    }

    [Fact]
    public void GetFilter_DevEuiWithSpecialChars_PreservesValue()
    {
        var repo = new TestableMeasurementRepositoryBase();
        var devEui = "A81758FFFE04D4F0";
        var (filterText, parameters) = repo.TestGetFilter(devEui, null, null);

        var dict = (Dictionary<string, object>)parameters;
        Assert.Equal(devEui, dict["devEui"]);
    }
}
