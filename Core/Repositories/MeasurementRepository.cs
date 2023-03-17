using Core.Entities;
using Microsoft.Extensions.Options;
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
    Task<Measurement[]> Get(string devEui, DateTime from, DateTime? till, CancellationToken cancellationToken);
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

    public async Task<Measurement[]> Get(string devEui, DateTime from, DateTime? till,
        CancellationToken cancellationToken)
    {
        string query;
        object parameters;

        if (till.HasValue)
        {
            query =
                "SELECT * FROM waterlevel WHERE DevEUI = $devEui AND time >= $from AND time <= $till GROUP BY * ORDER BY DESC LIMIT 1000";
            parameters = new { devEui, from, till };
        }
        else
        {
            query =
                "SELECT * FROM waterlevel WHERE DevEUI = $devEui AND time >= $from GROUP BY * ORDER BY DESC LIMIT 1000";
            parameters = new { devEui, from };
        }

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

        return record ?? throw new InvalidOperationException("Sensor database return no data");
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

    // ReSharper restore UnusedAutoPropertyAccessor.Local
}