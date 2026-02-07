using Core.Entities;
using Core.Repositories;
using Microsoft.Extensions.Options;
using Vibrant.InfluxDB.Client;
using Xunit;

namespace CoreTests.Repositories;

/// <summary>
/// Testable wrapper that exposes protected methods of MeasurementMoistureRepository.
/// </summary>
internal class TestableMeasurementMoistureRepository : MeasurementMoistureRepository
{
    public TestableMeasurementMoistureRepository()
        : base(Options.Create(new MeasurementInfluxOptions
        {
            Endpoint = new Uri("http://localhost:8086"),
            Username = "test",
            Password = "test"
        }))
    {
    }

    public string TestGetTableName() => GetTableName();

    public MeasurementMoisture TestReturnMeasurement(InfluxSeries<RecordMoisture> series, RecordMoisture record)
        => ReturnMeasurement(series, record);

    public AggregatedMeasurement TestReturnAggregatedMeasurement(InfluxSeries<AggregatedRecordMoisture> series, AggregatedRecordMoisture record)
        => ReturnAggregatedMeasurement(series, record);
}

public class MeasurementMoistureRepositoryTest
{
    private readonly TestableMeasurementMoistureRepository _repository = new();

    [Fact]
    public void GetTableName_ReturnsMoisture()
    {
        Assert.Equal("moisture", _repository.TestGetTableName());
    }

    [Fact]
    public void ReturnMeasurement_WithGroupedTags_UsesDevEuiFromTags()
    {
        var tags = new Dictionary<string, object> { { "DevEUI", "TAG_DEV_EUI" } };
        var series = new InfluxSeries<RecordMoisture>("moisture", tags);
        var record = new RecordMoisture
        {
            Timestamp = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc),
            DevEui = "RECORD_DEV_EUI",
            BatV = 3.4,
            SoilMoisturePrc = 65.5,
            SoilConductivity = 150,
            SoilTemperature = 18.2,
            Rssi = -90
        };

        var result = _repository.TestReturnMeasurement(series, record);

        Assert.Equal("TAG_DEV_EUI", result.DevEui);
        Assert.Equal(record.Timestamp, result.Timestamp);
        Assert.Equal(3.4, result.BatV);
        Assert.Equal(65.5, result.SoilMoisturePrc);
        Assert.Equal(150, result.SoilConductivity);
        Assert.Equal(18.2, result.SoilTemperature);
        Assert.Equal(-90, result.RssiDbm);
    }

    [Fact]
    public void ReturnMeasurement_WithOtherGroupedTags_UsesDevEuiFromRecord()
    {
        var tags = new Dictionary<string, object> { { "OtherTag", "value" } };
        var series = new InfluxSeries<RecordMoisture>("moisture", tags);
        var record = new RecordMoisture
        {
            Timestamp = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc),
            DevEui = "RECORD_DEV_EUI",
            BatV = 3.4,
            SoilMoisturePrc = 42.0,
            SoilConductivity = 100,
            SoilTemperature = 20.0,
            Rssi = -75
        };

        var result = _repository.TestReturnMeasurement(series, record);

        Assert.Equal("RECORD_DEV_EUI", result.DevEui);
        Assert.Equal(42.0, result.SoilMoisturePrc);
        Assert.Equal(100, result.SoilConductivity);
        Assert.Equal(20.0, result.SoilTemperature);
    }

    [Fact]
    public void ReturnMeasurement_WithEmptyGroupedTags_UsesDevEuiFromRecord()
    {
        var tags = new Dictionary<string, object>();
        var series = new InfluxSeries<RecordMoisture>("moisture", tags);
        var record = new RecordMoisture
        {
            Timestamp = new DateTime(2024, 7, 1, 8, 30, 0, DateTimeKind.Utc),
            DevEui = "FALLBACK_DEV_EUI",
            BatV = 3.2,
            SoilMoisturePrc = 30.0,
            SoilConductivity = 80,
            SoilTemperature = 15.0,
            Rssi = -60
        };

        var result = _repository.TestReturnMeasurement(series, record);

        Assert.Equal("FALLBACK_DEV_EUI", result.DevEui);
    }

    [Fact]
    public void ReturnAggregatedMeasurement_WithGroupedTags_UsesDevEuiFromTags()
    {
        var tags = new Dictionary<string, object> { { "DevEUI", "TAG_DEV_EUI" } };
        var series = new InfluxSeries<AggregatedRecordMoisture>("moisture", tags);
        var record = new AggregatedRecordMoisture
        {
            Timestamp = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc),
            DevEui = "RECORD_DEV_EUI",
            BatV = 3.4,
            Rssi = -90
        };

        var result = _repository.TestReturnAggregatedMeasurement(series, record);

        Assert.Equal("TAG_DEV_EUI", result.DevEui);
        Assert.Equal(record.Timestamp, result.Timestamp);
        Assert.Equal(3.4, result.BatV);
        Assert.Equal(-90, result.RssiDbm);
    }

    [Fact]
    public void ReturnAggregatedMeasurement_WithOtherGroupedTags_UsesDevEuiFromRecord()
    {
        var tags = new Dictionary<string, object> { { "OtherTag", "value" } };
        var series = new InfluxSeries<AggregatedRecordMoisture>("moisture", tags);
        var record = new AggregatedRecordMoisture
        {
            Timestamp = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc),
            DevEui = "RECORD_DEV_EUI",
            BatV = 3.4,
            Rssi = -90
        };

        var result = _repository.TestReturnAggregatedMeasurement(series, record);

        Assert.Equal("RECORD_DEV_EUI", result.DevEui);
    }
}
