using Core.Entities;
using Core.Repositories;
using Microsoft.Extensions.Options;
using Vibrant.InfluxDB.Client;
using Xunit;

namespace CoreTests.Repositories;

/// <summary>
/// Testable wrapper that exposes protected methods of MeasurementLevelRepository.
/// </summary>
internal class TestableMeasurementLevelRepository : MeasurementLevelRepository
{
    public TestableMeasurementLevelRepository()
        : base(Options.Create(new MeasurementInfluxOptions
        {
            Endpoint = new Uri("http://localhost:8086"),
            Username = "test",
            Password = "test"
        }))
    {
    }

    public string TestGetTableName() => GetTableName();

    public MeasurementLevel TestReturnMeasurement(InfluxSeries<RecordLevel> series, RecordLevel record)
        => ReturnMeasurement(series, record);

    public AggregatedMeasurementLevel TestReturnAggregatedMeasurement(InfluxSeries<AggregatedRecordLevel> series, AggregatedRecordLevel record)
        => ReturnAggregatedMeasurement(series, record);
}

public class MeasurementLevelRepositoryTest
{
    private readonly TestableMeasurementLevelRepository _repository = new();

    [Fact]
    public void GetTableName_ReturnsWaterlevel()
    {
        Assert.Equal("waterlevel", _repository.TestGetTableName());
    }

    [Fact]
    public void ReturnMeasurement_WithGroupedTags_UsesDevEuiFromTags()
    {
        var tags = new Dictionary<string, object> { { "DevEUI", "TAG_DEV_EUI" } };
        var series = new InfluxSeries<RecordLevel>("waterlevel", tags);
        var record = new RecordLevel
        {
            Timestamp = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc),
            DevEui = "RECORD_DEV_EUI",
            BatV = 3.6,
            Distance = 1500.0,
            Rssi = -80.0
        };

        var result = _repository.TestReturnMeasurement(series, record);

        Assert.Equal("TAG_DEV_EUI", result.DevEui);
        Assert.Equal(record.Timestamp, result.Timestamp);
        Assert.Equal(1500, result.DistanceMm);
        Assert.Equal(3.6, result.BatV);
        Assert.Equal(-80.0, result.RssiDbm);
    }

    [Fact]
    public void ReturnMeasurement_WithOtherGroupedTags_UsesDevEuiFromRecord()
    {
        var tags = new Dictionary<string, object> { { "OtherTag", "value" } };
        var series = new InfluxSeries<RecordLevel>("waterlevel", tags);
        var record = new RecordLevel
        {
            Timestamp = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc),
            DevEui = "RECORD_DEV_EUI",
            BatV = 3.6,
            Distance = 1500.0,
            Rssi = -80.0
        };

        var result = _repository.TestReturnMeasurement(series, record);

        Assert.Equal("RECORD_DEV_EUI", result.DevEui);
        Assert.Equal(record.Timestamp, result.Timestamp);
        Assert.Equal(1500, result.DistanceMm);
        Assert.Equal(3.6, result.BatV);
        Assert.Equal(-80.0, result.RssiDbm);
    }

    [Fact]
    public void ReturnMeasurement_WithEmptyGroupedTags_UsesDevEuiFromRecord()
    {
        var tags = new Dictionary<string, object>();
        var series = new InfluxSeries<RecordLevel>("waterlevel", tags);
        var record = new RecordLevel
        {
            Timestamp = new DateTime(2024, 7, 1, 8, 30, 0, DateTimeKind.Utc),
            DevEui = "FALLBACK_DEV_EUI",
            BatV = 3.2,
            Distance = 800.0,
            Rssi = -65.0
        };

        var result = _repository.TestReturnMeasurement(series, record);

        Assert.Equal("FALLBACK_DEV_EUI", result.DevEui);
        Assert.Equal(800, result.DistanceMm);
    }

    [Fact]
    public void ReturnMeasurement_DistanceCastToInt()
    {
        var tags = new Dictionary<string, object> { { "DevEUI", "DEV1" } };
        var series = new InfluxSeries<RecordLevel>("waterlevel", tags);
        var record = new RecordLevel
        {
            Timestamp = DateTime.UtcNow,
            DevEui = "DEV1",
            Distance = 1234.9
        };

        var result = _repository.TestReturnMeasurement(series, record);

        // int cast truncates
        Assert.Equal(1234, result.DistanceMm);
    }

    [Fact]
    public void ReturnAggregatedMeasurement_WithGroupedTags_UsesDevEuiFromTags()
    {
        var tags = new Dictionary<string, object> { { "DevEUI", "TAG_DEV_EUI" } };
        var series = new InfluxSeries<AggregatedRecordLevel>("waterlevel", tags);
        var record = new AggregatedRecordLevel
        {
            Timestamp = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc),
            DevEui = "RECORD_DEV_EUI",
            BatV = 3.6,
            MinDistance = 1200.0,
            MeanDistance = 1400.0,
            MaxDistance = 1800.0,
            LastDistance = 1500.0,
            Rssi = -80.0
        };

        var result = _repository.TestReturnAggregatedMeasurement(series, record);

        Assert.Equal("TAG_DEV_EUI", result.DevEui);
        Assert.Equal(record.Timestamp, result.Timestamp);
        Assert.Equal(1200, result.MinDistanceMm);
        Assert.Equal(1400, result.MeanDistanceMm);
        Assert.Equal(1800, result.MaxDistanceMm);
        Assert.Equal(1500, result.LastDistanceMm);
        Assert.Equal(3.6, result.BatV);
        Assert.Equal(-80.0, result.RssiDbm);
    }

    [Fact]
    public void ReturnAggregatedMeasurement_WithOtherGroupedTags_UsesDevEuiFromRecord()
    {
        var tags = new Dictionary<string, object> { { "OtherTag", "value" } };
        var series = new InfluxSeries<AggregatedRecordLevel>("waterlevel", tags);
        var record = new AggregatedRecordLevel
        {
            Timestamp = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc),
            DevEui = "RECORD_DEV_EUI",
            BatV = 3.6,
            MinDistance = 1200.0,
            MeanDistance = 1400.0,
            MaxDistance = 1800.0,
            LastDistance = 1500.0,
            Rssi = -80.0
        };

        var result = _repository.TestReturnAggregatedMeasurement(series, record);

        Assert.Equal("RECORD_DEV_EUI", result.DevEui);
    }

    [Fact]
    public void ReturnAggregatedMeasurement_NullDistances_ReturnsNulls()
    {
        var tags = new Dictionary<string, object> { { "DevEUI", "DEV1" } };
        var series = new InfluxSeries<AggregatedRecordLevel>("waterlevel", tags);
        var record = new AggregatedRecordLevel
        {
            Timestamp = DateTime.UtcNow,
            DevEui = "DEV1",
            BatV = 3.0,
            MinDistance = null,
            MeanDistance = null,
            MaxDistance = null,
            LastDistance = null,
            Rssi = -70.0
        };

        var result = _repository.TestReturnAggregatedMeasurement(series, record);

        Assert.Null(result.MinDistanceMm);
        Assert.Null(result.MeanDistanceMm);
        Assert.Null(result.MaxDistanceMm);
        Assert.Null(result.LastDistanceMm);
    }
}
