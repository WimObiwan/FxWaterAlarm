using Core.Entities;
using Core.Repositories;
using Microsoft.Extensions.Options;
using Vibrant.InfluxDB.Client;
using Xunit;

namespace CoreTests.Repositories;

/// <summary>
/// Testable wrapper that exposes protected methods of MeasurementThermometerRepository.
/// </summary>
internal class TestableMeasurementThermometerRepository : MeasurementThermometerRepository
{
    public TestableMeasurementThermometerRepository()
        : base(Options.Create(new MeasurementInfluxOptions
        {
            Endpoint = new Uri("http://localhost:8086"),
            Username = "test",
            Password = "test"
        }))
    {
    }

    public string TestGetTableName() => GetTableName();

    public MeasurementThermometer TestReturnMeasurement(InfluxSeries<RecordThermometer> series, RecordThermometer record)
        => ReturnMeasurement(series, record);

    public AggregatedMeasurement TestReturnAggregatedMeasurement(InfluxSeries<AggregatedRecordThermometer> series, AggregatedRecordThermometer record)
        => ReturnAggregatedMeasurement(series, record);
}

public class MeasurementThermometerRepositoryTest
{
    private readonly TestableMeasurementThermometerRepository _repository = new();

    [Fact]
    public void GetTableName_ReturnsThermometer()
    {
        Assert.Equal("thermometer", _repository.TestGetTableName());
    }

    [Fact]
    public void ReturnMeasurement_WithGroupedTags_UsesDevEuiFromTags()
    {
        var tags = new Dictionary<string, object> { { "DevEUI", "TAG_DEV_EUI" } };
        var series = new InfluxSeries<RecordThermometer>("thermometer", tags);
        var record = new RecordThermometer
        {
            Timestamp = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc),
            DevEui = "RECORD_DEV_EUI",
            BatV = 3.3,
            TempC = 22.5,
            HumPrc = 65.0,
            Rssi = -75
        };

        var result = _repository.TestReturnMeasurement(series, record);

        Assert.Equal("TAG_DEV_EUI", result.DevEui);
        Assert.Equal(record.Timestamp, result.Timestamp);
        Assert.Equal(3.3, result.BatV);
        Assert.Equal(22.5, result.TempC);
        Assert.Equal(65.0, result.HumPrc);
        Assert.Equal(-75, result.RssiDbm);
    }

    [Fact]
    public void ReturnMeasurement_WithOtherGroupedTags_UsesDevEuiFromRecord()
    {
        var tags = new Dictionary<string, object> { { "OtherTag", "value" } };
        var series = new InfluxSeries<RecordThermometer>("thermometer", tags);
        var record = new RecordThermometer
        {
            Timestamp = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc),
            DevEui = "RECORD_DEV_EUI",
            BatV = 3.3,
            TempC = 18.0,
            HumPrc = 50.0,
            Rssi = -80
        };

        var result = _repository.TestReturnMeasurement(series, record);

        Assert.Equal("RECORD_DEV_EUI", result.DevEui);
        Assert.Equal(18.0, result.TempC);
        Assert.Equal(50.0, result.HumPrc);
    }

    [Fact]
    public void ReturnMeasurement_WithEmptyGroupedTags_UsesDevEuiFromRecord()
    {
        var tags = new Dictionary<string, object>();
        var series = new InfluxSeries<RecordThermometer>("thermometer", tags);
        var record = new RecordThermometer
        {
            Timestamp = new DateTime(2024, 7, 1, 8, 30, 0, DateTimeKind.Utc),
            DevEui = "FALLBACK_DEV_EUI",
            BatV = 3.1,
            TempC = 25.0,
            HumPrc = 70.0,
            Rssi = -65
        };

        var result = _repository.TestReturnMeasurement(series, record);

        Assert.Equal("FALLBACK_DEV_EUI", result.DevEui);
    }

    [Fact]
    public void ReturnAggregatedMeasurement_WithGroupedTags_UsesDevEuiFromTags()
    {
        var tags = new Dictionary<string, object> { { "DevEUI", "TAG_DEV_EUI" } };
        var series = new InfluxSeries<AggregatedRecordThermometer>("thermometer", tags);
        var record = new AggregatedRecordThermometer
        {
            Timestamp = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc),
            DevEui = "RECORD_DEV_EUI",
            BatV = 3.3,
            Rssi = -75
        };

        var result = _repository.TestReturnAggregatedMeasurement(series, record);

        Assert.Equal("TAG_DEV_EUI", result.DevEui);
        Assert.Equal(record.Timestamp, result.Timestamp);
        Assert.Equal(3.3, result.BatV);
        Assert.Equal(-75, result.RssiDbm);
    }

    [Fact]
    public void ReturnAggregatedMeasurement_WithOtherGroupedTags_UsesDevEuiFromRecord()
    {
        var tags = new Dictionary<string, object> { { "OtherTag", "value" } };
        var series = new InfluxSeries<AggregatedRecordThermometer>("thermometer", tags);
        var record = new AggregatedRecordThermometer
        {
            Timestamp = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc),
            DevEui = "RECORD_DEV_EUI",
            BatV = 3.3,
            Rssi = -75
        };

        var result = _repository.TestReturnAggregatedMeasurement(series, record);

        Assert.Equal("RECORD_DEV_EUI", result.DevEui);
    }
}
