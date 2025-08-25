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

public abstract class MeasurementRepositoryBase<TRecord, TAggregatedRecord, TMeasurement, TAggregatedMeasurement> 
    where TMeasurement : Measurement
    where TAggregatedMeasurement : AggregatedMeasurement
    where TRecord : class, new()
    where TAggregatedRecord : class, new()
{
    private readonly MeasurementInfluxOptions _options;

    public MeasurementRepositoryBase(IOptions<MeasurementInfluxOptions> options)
    {
        _options = options.Value;
    }

    public async Task<TMeasurement?> GetLast(string devEui, CancellationToken cancellationToken)
    {
        using var influxClient = new InfluxClient(_options.Endpoint, _options.Username, _options.Password);
        var result = await influxClient.ReadAsync<TRecord>("wateralarm",
            $"SELECT * FROM {GetTableName()} WHERE DevEUI = $devEui GROUP BY * ORDER BY DESC LIMIT 1",
            new { devEui },
            cancellationToken);

        var series = result?.Results?.FirstOrDefault()?.Series?.FirstOrDefault();
        var record = series?.Rows?.FirstOrDefault();
        if (series == null || record == null)
            return null;

        return ReturnMeasurement(series, record);
    }

    public async Task<TMeasurement[]> Get(string devEui, DateTime? from, DateTime? till,
        CancellationToken cancellationToken)
    {
        (string filterText, object parameters) = GetFilter(devEui, from, till);

        var query = "SELECT"
                    + " *"
                    + $" FROM {GetTableName()}"
                    + filterText
                    + " GROUP BY *"
                    + " ORDER BY DESC LIMIT 1000";

        using var influxClient = new InfluxClient(_options.Endpoint, _options.Username, _options.Password);
        var result = await influxClient.ReadAsync<TRecord>("wateralarm", query, parameters,
            cancellationToken);

        var series = result?.Results?.FirstOrDefault()?.Series?.FirstOrDefault();
        var record = series?.Rows?.Select(record => ReturnMeasurement(series, record)).ToArray();

        return record ?? Array.Empty<TMeasurement>();
    }

    public async Task<AggregatedMeasurement[]> GetAggregated(string devEui, DateTime? from, DateTime? till, TimeSpan? interval,
        CancellationToken cancellationToken)
    {
        (string filterText, object parameters) = GetFilter(devEui, from, till);

        string? groupByText;
        if (interval is {} interval2)
        {
            string intervalText;
            if (interval < TimeSpan.FromHours(1))
                intervalText = $"{60 / (60 / (int)interval2.TotalMinutes)}m";
            else if (interval < TimeSpan.FromDays(1))
                intervalText = $"{24 / (24 / (int)interval2.TotalHours)}h";
            else
                intervalText = $"{(int)interval2.TotalDays}d";

            var timeZoneOffset = (int)TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow).TotalSeconds;
            groupByText = $", time({intervalText}, -{timeZoneOffset}s)";
        }
        else
        {
            groupByText = null;
        }

        var query = "SELECT "
                    + " MIN(*), MEAN(*), MAX(*), LAST(*)"
                    + " FROM waterlevel"
                    + filterText
                    + " GROUP BY *"
                    + groupByText
                    + " ORDER BY DESC LIMIT 1000";

        
        using var influxClient = new InfluxClient(_options.Endpoint, _options.Username, _options.Password);
        var result = await influxClient.ReadAsync<TAggregatedRecord>("wateralarm", query, parameters,
            cancellationToken);

        var series = result?.Results?.FirstOrDefault()?.Series?.FirstOrDefault();
        var record = series?.Rows?.Select(record => ReturnAggregatedMeasurement(series, record)).ToArray();
            // new AggregatedMeasurement
            // {
            //     DevEui = (string)series.GroupedTags["DevEUI"],
            //     Timestamp = record.Timestamp,
            //     MinDistanceMm = record.MinDistance,
            //     MeanDistanceMm = record.MeanDistance,
            //     MaxDistanceMm = record.MaxDistance,
            //     LastDistanceMm = record.LastDistance,
            //     BatV = record.BatV,
            //     RssiDbm = record.Rssi
            // }).ToArray();

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

    public async Task<TMeasurement?> GetLastBefore(string devEui, DateTime timestamp,
        CancellationToken cancellationToken)
    {
        using var influxClient = new InfluxClient(_options.Endpoint, _options.Username, _options.Password);
        var result = await influxClient.ReadAsync<TRecord>("wateralarm",
            $"SELECT * FROM {GetTableName()} WHERE DevEUI = $devEui AND time <= $before GROUP BY * ORDER BY DESC LIMIT 1",
            new { devEui, before = timestamp },
            cancellationToken);
        var series = result?.Results?.FirstOrDefault()?.Series?.FirstOrDefault();
        var record = series?.Rows?.FirstOrDefault();
        if (series == null || record == null)
            return null;

        return ReturnMeasurement(series, record);
    }

    public async Task<AggregatedMeasurement?> GetLastMedian(string devEui, DateTime from, CancellationToken cancellationToken)
    {
        var result = await GetAggregated(devEui, from, null, null, cancellationToken);
        if (result.Length > 0)
            return result[0];
        return null;
    }

    // ReSharper disable UnusedAutoPropertyAccessor.Local

    private class AggregatedRecordLevel
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

    public async Task Write(TRecord record, CancellationToken cancellationToken)
    {
        using var influxClient = new InfluxClient(_options.Endpoint, _options.Username, _options.Password);
        await influxClient.WriteAsync("wateralarm", GetTableName(), new[] { record }, cancellationToken);
    }

    public async Task<TMeasurement[]> GetMeasurementsInTimeRange(string devEui, DateTime from, DateTime till, CancellationToken cancellationToken)
    {
        using var influxClient = new InfluxClient(_options.Endpoint, _options.Username, _options.Password);
        var result = await influxClient.ReadAsync<TRecord>("wateralarm",
            $"SELECT * FROM {GetTableName()} WHERE DevEUI = $devEui AND time >= $from AND time <= $till ORDER BY time",
            new { devEui, from, till },
            cancellationToken);

        var measurements = new List<TMeasurement>();
        if (result?.Results?.FirstOrDefault()?.Series != null)
        {
            foreach (var series in result.Results.First().Series)
            {
                if (series.Rows != null)
                {
                    measurements.AddRange(series.Rows.Select(record => ReturnMeasurement(series, record)));
                }
            }
        }

        return measurements.ToArray();
    }

    public async Task DeleteMeasurementsInTimeRange(string devEui, DateTime from, DateTime till, CancellationToken cancellationToken)
    {
        // InfluxDB v1.x does support DELETE, but we need to use the HTTP API directly
        // since Vibrant.InfluxDB.Client doesn't expose delete operations
        
        using var httpClient = new HttpClient();
        var endpoint = _options.Endpoint;
        
        // Format timestamps for InfluxDB (RFC3339 format)
        var fromTimestamp = from.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        var tillTimestamp = till.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        
        // Construct the delete query
        var deleteQuery = $"DELETE FROM {GetTableName()} WHERE \"DevEUI\" = '{devEui}' AND time >= '{fromTimestamp}' AND time <= '{tillTimestamp}'";
        
        // Create the request URL
        var requestUrl = $"{endpoint}/query?db=wateralarm";
        if (!string.IsNullOrEmpty(_options.Username))
        {
            requestUrl += $"&u={_options.Username}&p={_options.Password}";
        }
        
        // Create form content with the query
        var formContent = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("q", deleteQuery)
        });
        
        // Execute the delete request
        var response = await httpClient.PostAsync(requestUrl, formContent, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Failed to delete measurement from InfluxDB: {response.StatusCode} - {errorContent}");
        }
    }

    protected abstract string GetTableName();
    protected abstract TMeasurement ReturnMeasurement(InfluxSeries<TRecord> series, TRecord record);
    protected abstract TAggregatedMeasurement ReturnAggregatedMeasurement(InfluxSeries<TAggregatedRecord> series, TAggregatedRecord record);
}