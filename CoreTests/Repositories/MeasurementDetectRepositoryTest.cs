using Core.Entities;
using Core.Repositories;
using Microsoft.Extensions.Options;
using Vibrant.InfluxDB.Client;
using Xunit;

namespace CoreTests.Repositories;

/// <summary>
/// Testable wrapper that exposes protected methods of MeasurementDetectRepository.
/// </summary>
internal class TestableMeasurementDetectRepository : MeasurementDetectRepository
{
    public TestableMeasurementDetectRepository()
        : base(Options.Create(new MeasurementInfluxOptions
        {
            Endpoint = new Uri("http://localhost:8086"),
            Username = "test",
            Password = "test"
        }))
    {
    }

    public string TestGetTableName() => GetTableName();

    public MeasurementDetect TestReturnMeasurement(InfluxSeries<RecordDetect> series, RecordDetect record)
        => ReturnMeasurement(series, record);

    public AggregatedMeasurement TestReturnAggregatedMeasurement(InfluxSeries<AggregatedRecordDetect> series, AggregatedRecordDetect record)
        => ReturnAggregatedMeasurement(series, record);
}

public class MeasurementDetectRepositoryTest
{
    private readonly TestableMeasurementDetectRepository _repository = new();

    [Fact]
    public void GetTableName_ReturnsDetect()
    {
        Assert.Equal("detect", _repository.TestGetTableName());
    }

    [Fact]
    public void ReturnMeasurement_WithGroupedTags_UsesDevEuiFromTags()
    {
        var tags = new Dictionary<string, object> { { "DevEUI", "TAG_DEV_EUI" } };
        var series = new InfluxSeries<RecordDetect>("detect", tags);
        var record = new RecordDetect
        {
            Timestamp = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc),
            DevEui = "RECORD_DEV_EUI",
            BatV = 3.5,
            Status = 1,
            Rssi = -85
        };

        var result = _repository.TestReturnMeasurement(series, record);

        Assert.Equal("TAG_DEV_EUI", result.DevEui);
        Assert.Equal(record.Timestamp, result.Timestamp);
        Assert.Equal(1, result.Status);
        Assert.Equal(3.5, result.BatV);
        Assert.Equal(-85, result.RssiDbm);
    }

    [Fact]
    public void ReturnMeasurement_WithOtherGroupedTags_UsesDevEuiFromRecord()
    {
        var tags = new Dictionary<string, object> { { "OtherTag", "value" } };
        var series = new InfluxSeries<RecordDetect>("detect", tags);
        var record = new RecordDetect
        {
            Timestamp = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc),
            DevEui = "RECORD_DEV_EUI",
            BatV = 3.5,
            Status = 0,
            Rssi = -90
        };

        var result = _repository.TestReturnMeasurement(series, record);

        Assert.Equal("RECORD_DEV_EUI", result.DevEui);
        Assert.Equal(0, result.Status);
    }

    [Fact]
    public void ReturnMeasurement_WithEmptyGroupedTags_UsesDevEuiFromRecord()
    {
        var tags = new Dictionary<string, object>();
        var series = new InfluxSeries<RecordDetect>("detect", tags);
        var record = new RecordDetect
        {
            Timestamp = new DateTime(2024, 7, 1, 8, 30, 0, DateTimeKind.Utc),
            DevEui = "FALLBACK_DEV_EUI",
            BatV = 3.3,
            Status = 1,
            Rssi = -70
        };

        var result = _repository.TestReturnMeasurement(series, record);

        Assert.Equal("FALLBACK_DEV_EUI", result.DevEui);
    }

    [Fact]
    public void ReturnAggregatedMeasurement_WithGroupedTags_UsesDevEuiFromTags()
    {
        var tags = new Dictionary<string, object> { { "DevEUI", "TAG_DEV_EUI" } };
        var series = new InfluxSeries<AggregatedRecordDetect>("detect", tags);
        var record = new AggregatedRecordDetect
        {
            Timestamp = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc),
            DevEui = "RECORD_DEV_EUI",
            BatV = 3.5,
            Rssi = -85
        };

        var result = _repository.TestReturnAggregatedMeasurement(series, record);

        Assert.Equal("TAG_DEV_EUI", result.DevEui);
        Assert.Equal(record.Timestamp, result.Timestamp);
        Assert.Equal(3.5, result.BatV);
        Assert.Equal(-85, result.RssiDbm);
    }

    [Fact]
    public void ReturnAggregatedMeasurement_WithOtherGroupedTags_UsesDevEuiFromRecord()
    {
        var tags = new Dictionary<string, object> { { "OtherTag", "value" } };
        var series = new InfluxSeries<AggregatedRecordDetect>("detect", tags);
        var record = new AggregatedRecordDetect
        {
            Timestamp = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc),
            DevEui = "RECORD_DEV_EUI",
            BatV = 3.5,
            Rssi = -85
        };

        var result = _repository.TestReturnAggregatedMeasurement(series, record);

        Assert.Equal("RECORD_DEV_EUI", result.DevEui);
    }
}
