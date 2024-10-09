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
}

public class MeasurementLevelRepository : MeasurementRepositoryBase<RecordLevel, AggregatedRecordLevel, MeasurementLevel, AggregatedMeasurementLevel>, IMeasurementLevelRepository
{
    private readonly MeasurementInfluxOptions _options;

    public MeasurementLevelRepository(IOptions<MeasurementInfluxOptions> options)
    : base(options)
    {
        _options = options.Value;
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
            DistanceMm = record.Distance,
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
            MinDistanceMm = record.MinDistance,
            MeanDistanceMm = record.MeanDistance,
            MaxDistanceMm = record.MaxDistance,
            LastDistanceMm = record.LastDistance,
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

    [InfluxField("distance")] public int Distance { get; set; }

    [InfluxField("RSSI")] public int Rssi { get; set; }
}

public class AggregatedRecordLevel
{
    [InfluxTimestamp] public DateTime Timestamp { get; set; }

    [InfluxTag("DevEUI")] public string DevEui { get; set; } = default!;

    [InfluxField("last_batV")] public double BatV { get; set; }

    [InfluxField("min_distance")] public int? MinDistance { get; set; }
    [InfluxField("mean_distance")] public int? MeanDistance { get; set; }
    [InfluxField("max_distance")] public int? MaxDistance { get; set; }
    [InfluxField("last_distance")] public int? LastDistance { get; set; }

    [InfluxField("last_RSSI")] public int Rssi { get; set; }
}

// ReSharper restore UnusedAutoPropertyAccessor.Local
