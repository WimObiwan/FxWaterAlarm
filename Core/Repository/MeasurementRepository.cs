using Core.Entities;
using Microsoft.Extensions.Options;
using Vibrant.InfluxDB.Client;

namespace Core.Repository;

public class MeasurementInfluxOptions
{
    public const string Position = "Influx";

    public Uri Endpoint { get; set; } = default!;
    public string Username { get; set; } = default!;
    public string Password { get; set; } = default!;
}

public interface IMeasurementRepository
{
    Task<Measurement?> GetLast(string devEui);
}
    
public class MeasurementRepository : IMeasurementRepository
{
    private readonly MeasurementInfluxOptions _options;

    private class Record
     {
         [InfluxTimestamp]
         public DateTime Timestamp { get; set; }

         [InfluxTag("DevEUI")]
         public string DevEui { get; set; } = default!;

         [InfluxField("batV")]
         public double BatV { get; set; }

         [InfluxField("distance")]
         public int Distance { get; set; }

         [InfluxField("RSSI")]
         public int Rssi { get; set; }
     }

    public MeasurementRepository(IOptions<MeasurementInfluxOptions> options)
    {
        _options = options.Value;
    }
    
    public async Task<Measurement?> GetLast(string devEui)
    {
        using var influxClient = new InfluxClient(_options.Endpoint, _options.Username, _options.Password);
        var result = await influxClient.ReadAsync<Record>("wateralarm", "SELECT * FROM waterlevel WHERE DevEUI = $devEui GROUP BY * ORDER BY DESC LIMIT 1",
            new { devEui = devEui });
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
}