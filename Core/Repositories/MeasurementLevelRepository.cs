using System.Text;
using Core.Entities;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Vibrant.InfluxDB.Client;

namespace Core.Repositories;

public interface IMeasurementLevelRepository
{
    Task<MeasurementLevel?> GetLast(string devEui, CancellationToken cancellationToken);

    Task<MeasurementLevel[]> Get(string devEui, DateTime? from, DateTime? till,
        CancellationToken cancellationToken);

    Task<AggregatedMeasurement[]> GetAggregated(string devEui, DateTime? from, DateTime? till, TimeSpan? interval,
        CancellationToken cancellationToken);

    Task<MeasurementLevel?> GetLastBefore(string devEui, DateTime dateTime,
        CancellationToken cancellationToken);

    Task<AggregatedMeasurement?> GetLastMedian(string devEui, DateTime from, CancellationToken cancellationToken);

    Task Write(RecordLevel record, CancellationToken cancellationToken);
}

public class MeasurementLevelRepository : MeasurementRepositoryBase<RecordLevel, AggregatedRecordLevel, MeasurementLevel, AggregatedMeasurementLevel>, IMeasurementLevelRepository
{
    public MeasurementLevelRepository(IOptions<MeasurementInfluxOptions> options)
    : base(options)
    {
    }

    protected override string GetTableName()
    {
        return "waterlevel";
    }

    protected override MeasurementLevel ReturnMeasurement(InfluxSeries<RecordLevel> series, RecordLevel record)
    {
        return new MeasurementLevel
        {
            DevEui = (string)series.GroupedTags["DevEUI"],
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
            DevEui = (string)series.GroupedTags["DevEUI"],
            Timestamp = record.Timestamp,
            MinDistanceMm = (int?)record.MinDistance,
            MeanDistanceMm = (int?)record.MeanDistance,
            MaxDistanceMm = (int?)record.MaxDistance,
            LastDistanceMm = (int?)record.LastDistance,
            BatV = record.BatV,
            RssiDbm = record.Rssi
        };
    }
}

// ReSharper disable UnusedAutoPropertyAccessor.Local

public class RecordLevel
{
    [InfluxTimestamp] public DateTime Timestamp { get; set; }

    [InfluxTag("DevEUI")] public string DevEui { get; set; } = default!;

    [InfluxField("batV")] public double BatV { get; set; }

    [InfluxField("distance")] public double Distance { get; set; }

    [InfluxField("RSSI")] public double Rssi { get; set; }
}

public class AggregatedRecordLevel
{
    [InfluxTimestamp] public DateTime Timestamp { get; set; }

    [InfluxTag("DevEUI")] public string DevEui { get; set; } = default!;

    [InfluxField("last_batV")] public double BatV { get; set; }

    [InfluxField("min_distance")] public double? MinDistance { get; set; }
    [InfluxField("mean_distance")] public double? MeanDistance { get; set; }
    [InfluxField("max_distance")] public double? MaxDistance { get; set; }
    [InfluxField("last_distance")] public double? LastDistance { get; set; }

    [InfluxField("last_RSSI")] public double Rssi { get; set; }
}

// ReSharper restore UnusedAutoPropertyAccessor.Local
