using System.Text;
using Core.Entities;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Vibrant.InfluxDB.Client;

namespace Core.Repositories;

public interface IMeasurementThermometerRepository
{
    Task<MeasurementThermometer?> GetLast(string devEui, CancellationToken cancellationToken);

    Task<MeasurementThermometer[]> Get(string devEui, DateTime? from, DateTime? till,
        CancellationToken cancellationToken);

    Task<AggregatedMeasurement[]> GetAggregated(string devEui, DateTime? from, DateTime? till, TimeSpan? interval,
        CancellationToken cancellationToken);

    Task<MeasurementThermometer?> GetLastBefore(string devEui, DateTime dateTime,
        CancellationToken cancellationToken);

    Task<AggregatedMeasurement?> GetLastMedian(string devEui, DateTime from, CancellationToken cancellationToken);

    Task Write(RecordThermometer record, CancellationToken cancellationToken);

    Task<MeasurementThermometer[]> GetMeasurementsInTimeRange(string devEui, DateTime from, DateTime till, CancellationToken cancellationToken);

    Task DeleteMeasurementsInTimeRange(string devEui, DateTime from, DateTime till, CancellationToken cancellationToken);
}

public class MeasurementThermometerRepository : MeasurementRepositoryBase<RecordThermometer, AggregatedRecordThermometer, MeasurementThermometer, AggregatedMeasurement>, IMeasurementThermometerRepository
{
    public MeasurementThermometerRepository(IOptions<MeasurementInfluxOptions> options)
    : base(options)
    {
    }

   protected override string GetTableName()
    {
        return "thermometer";
    }

    protected override MeasurementThermometer ReturnMeasurement(InfluxSeries<RecordThermometer> series, RecordThermometer record)
    {
        return new MeasurementThermometer
        {
            DevEui = (string)series.GroupedTags["DevEUI"],
            Timestamp = record.Timestamp,
            BatV = record.BatV,
            RssiDbm = record.Rssi,
            TempC = record.TempC,
            HumPrc = record.HumPrc,
        };
    }

    protected override AggregatedMeasurement ReturnAggregatedMeasurement(InfluxSeries<AggregatedRecordThermometer> series, AggregatedRecordThermometer record)
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

public class RecordThermometer
{
    [InfluxTimestamp] public DateTime Timestamp { get; set; }

    [InfluxTag("DevEUI")] public string DevEui { get; set; } = default!;

    [InfluxField("batV")] public double BatV { get; set; }

    [InfluxField("tempC")] public double TempC { get; set; }
    [InfluxField("humPrc")] public double HumPrc { get; set; }

    [InfluxField("RSSI")] public int Rssi { get; set; }
}

public class AggregatedRecordThermometer
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
