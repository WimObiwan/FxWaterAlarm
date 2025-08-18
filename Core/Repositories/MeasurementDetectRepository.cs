using System.Text;
using Core.Entities;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Vibrant.InfluxDB.Client;

namespace Core.Repositories;

public interface IMeasurementDetectRepository
{
    Task<MeasurementDetect?> GetLast(string devEui, CancellationToken cancellationToken);

    Task<MeasurementDetect[]> Get(string devEui, DateTime? from, DateTime? till,
        CancellationToken cancellationToken);

    Task<AggregatedMeasurement[]> GetAggregated(string devEui, DateTime? from, DateTime? till, TimeSpan? interval,
        CancellationToken cancellationToken);

    Task<MeasurementDetect?> GetLastBefore(string devEui, DateTime dateTime,
        CancellationToken cancellationToken);

    Task<AggregatedMeasurement?> GetLastMedian(string devEui, DateTime from, CancellationToken cancellationToken);

    Task Write(RecordDetect record, CancellationToken cancellationToken);
}

public class MeasurementDetectRepository : MeasurementRepositoryBase<RecordDetect, AggregatedRecordDetect, MeasurementDetect, AggregatedMeasurement>, IMeasurementDetectRepository
{
    public MeasurementDetectRepository(IOptions<MeasurementInfluxOptions> options)
    : base(options)
    {
    }

    protected override string GetTableName()
    {
        return "detect";
    }

    protected override MeasurementDetect ReturnMeasurement(InfluxSeries<RecordDetect> series, RecordDetect record)
    {
        return new MeasurementDetect
        {
            DevEui = (string)series.GroupedTags["DevEUI"],
            Timestamp = record.Timestamp,
            Status = record.Status,
            BatV = record.BatV,
            RssiDbm = record.Rssi
        };
    }

    protected override AggregatedMeasurement ReturnAggregatedMeasurement(InfluxSeries<AggregatedRecordDetect> series, AggregatedRecordDetect record)
    {
        return new AggregatedMeasurement
        {
            DevEui = (string)series.GroupedTags["DevEUI"],
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

public class RecordDetect
{
    [InfluxTimestamp] public DateTime Timestamp { get; set; }

    [InfluxTag("DevEUI")] public string DevEui { get; set; } = default!;

    [InfluxField("batV")] public double BatV { get; set; }

    [InfluxField("waterStatus")] public int Status { get; set; }

    [InfluxField("RSSI")] public int Rssi { get; set; }
}

public class AggregatedRecordDetect
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
