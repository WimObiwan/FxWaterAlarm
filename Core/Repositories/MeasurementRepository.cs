using System.Text;
using Core.Entities;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Vibrant.InfluxDB.Client;

namespace Core.Repositories;

public class MeasurementInfluxOptions
{
    public const string Position = "Influx";

    public Uri Endpoint { get; set; } = default!;
    public string Username { get; set; } = default!;
    public string Password { get; set; } = default!;
}

public interface IMeasurementRepository
{
    Task<Measurement?> GetLast(string devEui, CancellationToken cancellationToken);

    Task<Measurement[]> Get(string devEui, DateTime? from, DateTime? till,
        CancellationToken cancellationToken);

    Task<AggregatedMeasurement[]> GetAggregated(string devEui, DateTime? from, DateTime? till, TimeSpan interval,
        CancellationToken cancellationToken);

    Task<Measurement?> GetLastBefore(string devEui, DateTime dateTime,
        CancellationToken cancellationToken);
}

public class MeasurementRepository : IMeasurementRepository
{
    private readonly MeasurementInfluxOptions _options;

    public MeasurementRepository(IOptions<MeasurementInfluxOptions> options)
    {
        _options = options.Value;
    }

    public async Task<Measurement?> GetLast(string devEui, CancellationToken cancellationToken)
    {
        using var influxClient = new InfluxClient(_options.Endpoint, _options.Username, _options.Password);
        var result = await influxClient.ReadAsync<Record>("wateralarm",
            "SELECT * FROM waterlevel WHERE DevEUI = $devEui GROUP BY * ORDER BY DESC LIMIT 1",
            new { devEui },
            cancellationToken);
        var series = result?.Results?.FirstOrDefault()?.Series?.FirstOrDefault();
        var record = series?.Rows?.FirstOrDefault();
        if (series == null || record == null)
            return null;

        return new Measurement
        {
            DevEui = (string)series.GroupedTags["DevEUI"],
            Timestamp = record.Timestamp,
            DistanceMm = record.Distance,
            BatV = record.BatV,
            RssiDbm = record.Rssi
        };
    }

    public async Task<Measurement[]> Get(string devEui, DateTime? from, DateTime? till,
        CancellationToken cancellationToken)
    {
        (string filterText, object parameters) = GetFilter(devEui, from, till);

        var query = "SELECT"
                    + " *"
                    + " FROM waterlevel"
                    + filterText
                    + " GROUP BY *"
                    + " ORDER BY DESC LIMIT 1000";

        using var influxClient = new InfluxClient(_options.Endpoint, _options.Username, _options.Password);
        var result = await influxClient.ReadAsync<Record>("wateralarm", query, parameters,
            cancellationToken);

        var series = result?.Results?.FirstOrDefault()?.Series?.FirstOrDefault();
        var record = series?.Rows?.Select(record =>
            new Measurement
            {
                DevEui = (string)series.GroupedTags["DevEUI"],
                Timestamp = record.Timestamp,
                DistanceMm = record.Distance,
                BatV = record.BatV,
                RssiDbm = record.Rssi
            }).ToArray();

        return record ?? Array.Empty<Measurement>();
    }

    public async Task<AggregatedMeasurement[]> GetAggregated(string devEui, DateTime? from, DateTime? till, TimeSpan interval,
        CancellationToken cancellationToken)
    {
        (string filterText, object parameters) = GetFilter(devEui, from, till);

        string intervalText;
        if (interval < TimeSpan.FromHours(1))
            intervalText = $", time({60 / (60 / (int)interval.TotalMinutes)}m)";
        else if (interval < TimeSpan.FromDays(1))
            intervalText = $", time({24 / (24 / (int)interval.TotalHours)}h)";
        else
            intervalText = $", time({(int)interval.TotalDays}d)";

        var query = "SELECT "
                    + " MIN(*), MEAN(*), MAX(*), LAST(*)"
                    + " FROM waterlevel"
                    + filterText
                    + " GROUP BY *"
                    + intervalText
                    + " ORDER BY DESC LIMIT 1000";

        using var influxClient = new InfluxClient(_options.Endpoint, _options.Username, _options.Password);
        var result = await influxClient.ReadAsync<RecordAgg>("wateralarm", query, parameters,
            cancellationToken);

        var series = result?.Results?.FirstOrDefault()?.Series?.FirstOrDefault();
        var record = series?.Rows?.Select(record =>
            new AggregatedMeasurement
            {
                DevEui = (string)series.GroupedTags["DevEUI"],
                Timestamp = record.Timestamp,
                MinDistanceMm = record.MinDistance,
                MeanDistanceMm = record.MeanDistance,
                MaxDistanceMm = record.MaxDistance,
                LastDistanceMm = record.LastDistance,
                BatV = record.BatV,
                RssiDbm = record.Rssi
            }).ToArray();

        return record ?? Array.Empty<AggregatedMeasurement>();
    }

    private (string, object) GetFilter(string devEui, DateTime? from, DateTime? till)
    {
        StringBuilder filterText = new();
        Dictionary<string, object> parameters = new();

        filterText.Append(" WHERE");
        filterText.Append(" DevEUI = $devEui");
        parameters.Add("devEui", devEui);
        
        if (from.HasValue)
        {
            filterText.Append(" AND time >= $from");
            parameters.Add("from", from.Value);
        }
        
        if (till.HasValue)
        {
            filterText.Append(" AND time < $till");
            parameters.Add("till", till.Value);
        }

        return (filterText.ToString(), parameters);
    }

    public async Task<Measurement?> GetLastBefore(string devEui, DateTime timestamp,
        CancellationToken cancellationToken)
    {
        using var influxClient = new InfluxClient(_options.Endpoint, _options.Username, _options.Password);
        var result = await influxClient.ReadAsync<Record>("wateralarm",
            "SELECT * FROM waterlevel WHERE DevEUI = $devEui AND time <= $before GROUP BY * ORDER BY DESC LIMIT 1",
            new { devEui, before = timestamp },
            cancellationToken);
        var series = result?.Results?.FirstOrDefault()?.Series?.FirstOrDefault();
        var record = series?.Rows?.FirstOrDefault();
        if (series == null || record == null)
            return null;

        return new Measurement
        {
            DevEui = (string)series.GroupedTags["DevEUI"],
            Timestamp = record.Timestamp,
            DistanceMm = record.Distance,
            BatV = record.BatV,
            RssiDbm = record.Rssi
        };
    }

    // ReSharper disable UnusedAutoPropertyAccessor.Local

    private class Record
    {
        [InfluxTimestamp] public DateTime Timestamp { get; set; }

        [InfluxTag("DevEUI")] public string DevEui { get; set; } = default!;

        [InfluxField("batV")] public double BatV { get; set; }

        [InfluxField("distance")] public int Distance { get; set; }

        [InfluxField("RSSI")] public int Rssi { get; set; }
    }

    private class RecordAgg
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
}