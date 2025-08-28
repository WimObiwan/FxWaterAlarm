using System.Text;
using Core.Entities;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Vibrant.InfluxDB.Client;

namespace Core.Repositories;

public interface IMeasurementMoistureRepository
{
    Task<MeasurementMoisture?> GetLast(string devEui, CancellationToken cancellationToken);

    Task<MeasurementMoisture[]> Get(string devEui, DateTime? from, DateTime? till,
        CancellationToken cancellationToken);

    Task<AggregatedMeasurement[]> GetAggregated(string devEui, DateTime? from, DateTime? till, TimeSpan? interval,
        CancellationToken cancellationToken);

    Task<MeasurementMoisture?> GetLastBefore(string devEui, DateTime dateTime,
        CancellationToken cancellationToken);

    Task<AggregatedMeasurement?> GetLastMedian(string devEui, DateTime from, CancellationToken cancellationToken);

    Task Write(RecordMoisture record, CancellationToken cancellationToken);

    Task<MeasurementMoisture[]> GetMeasurementsInTimeRange(string devEui, DateTime from, DateTime till, CancellationToken cancellationToken);

    Task DeleteMeasurementsInTimeRange(string devEui, DateTime from, DateTime till, CancellationToken cancellationToken);
}

public class MeasurementMoistureRepository : MeasurementRepositoryBase<RecordMoisture, AggregatedRecordMoisture, MeasurementMoisture, AggregatedMeasurement>, IMeasurementMoistureRepository
{
    public MeasurementMoistureRepository(IOptions<MeasurementInfluxOptions> options)
    : base(options)
    {
    }

   protected override string GetTableName()
    {
        return "moisture";
    }

    protected override MeasurementMoisture ReturnMeasurement(InfluxSeries<RecordMoisture> series, RecordMoisture record)
    {
        string devEui;
        if (series.GroupedTags.TryGetValue("DevEUI", out var devEuiObject))
            devEui = (string)devEuiObject;
        else
            devEui = record.DevEui;

        return new MeasurementMoisture
        {
            DevEui = devEui,
            Timestamp = record.Timestamp,
            BatV = record.BatV,
            RssiDbm = record.Rssi,
            SoilMoisturePrc = record.SoilMoisturePrc,
            SoilConductivity = record.SoilConductivity,
            SoilTemperature = record.SoilTemperature
        };
    }

    protected override AggregatedMeasurement ReturnAggregatedMeasurement(InfluxSeries<AggregatedRecordMoisture> series, AggregatedRecordMoisture record)
    {
        string devEui;
        if (series.GroupedTags.TryGetValue("DevEUI", out var devEuiObject))
            devEui = (string)devEuiObject;
        else
            devEui = record.DevEui;

        return new AggregatedMeasurement
        {
            DevEui = devEui,
            Timestamp = record.Timestamp,
            // MinDistanceMm = record.MinDistance,
            // MeanDistanceMm = record.MeanDistance,
            // MaxDistanceMm = record.MaxDistance,
            // LastDistanceMm = record.LastDistance,
            BatV = record.BatV,
            RssiDbm = record.Rssi
        };
    }
}

// ReSharper disable UnusedAutoPropertyAccessor.Local

public class RecordMoisture
{
    [InfluxTimestamp] public DateTime Timestamp { get; set; }

    [InfluxTag("DevEUI")] public string DevEui { get; set; } = default!;

    [InfluxField("batV")] public double BatV { get; set; }

    [InfluxField("soilConductivity")] public int SoilConductivity { get; set; }
    [InfluxField("soilMoisturePrc")] public double SoilMoisturePrc { get; set; }
    [InfluxField("soilTemperature")] public double SoilTemperature { get; set; }

    [InfluxField("RSSI")] public int Rssi { get; set; }
}

public class AggregatedRecordMoisture
{
    [InfluxTimestamp] public DateTime Timestamp { get; set; }

    [InfluxTag("DevEUI")] public string DevEui { get; set; } = default!;

    [InfluxField("last_batV")] public double BatV { get; set; }

    // [InfluxField("min_distance")] public int? MinDistance { get; set; }
    // [InfluxField("mean_distance")] public int? MeanDistance { get; set; }
    // [InfluxField("max_distance")] public int? MaxDistance { get; set; }
    // [InfluxField("last_distance")] public int? LastDistance { get; set; }

    [InfluxField("last_RSSI")] public int Rssi { get; set; }
}

// ReSharper restore UnusedAutoPropertyAccessor.Local
