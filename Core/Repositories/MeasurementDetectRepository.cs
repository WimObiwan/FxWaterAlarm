// using System.Text;
// using Core.Entities;
// using Microsoft.Extensions.Options;
// using Microsoft.Extensions.Primitives;
// using Vibrant.InfluxDB.Client;

// namespace Core.Repositories;

// public interface IMeasurementDetectRepository
// {
//     Task<MeasurementDetect?> GetLast(string devEui, CancellationToken cancellationToken);

//     Task<MeasurementDetect[]> Get(string devEui, DateTime? from, DateTime? till,
//         CancellationToken cancellationToken);

//     Task<AggregatedMeasurement[]> GetAggregated(string devEui, DateTime? from, DateTime? till, TimeSpan? interval,
//         CancellationToken cancellationToken);

//     Task<MeasurementDetect?> GetLastBefore(string devEui, DateTime dateTime,
//         CancellationToken cancellationToken);

//     Task<AggregatedMeasurement?> GetLastMedian(string devEui, DateTime from, CancellationToken cancellationToken);
// }

// public class MeasurementDetectRepository : MeasurementRepositoryBase<MeasurementDetect>, IMeasurementDetectRepository
// {
//     private readonly MeasurementInfluxOptions _options;

//     public MeasurementDetectRepository(IOptions<MeasurementInfluxOptions> options)
//     {
//         _options = options.Value;
//     }

//     // public async Task<MeasurementDetect?> GetLast(string devEui, CancellationToken cancellationToken)
//     // {
//     //     using var influxClient = new InfluxClient(_options.Endpoint, _options.Username, _options.Password);
//     //     var result = await influxClient.ReadAsync<RecordLevel>("wateralarm",
//     //         "SELECT * FROM waterlevel WHERE DevEUI = $devEui GROUP BY * ORDER BY DESC LIMIT 1",
//     //         new { devEui },
//     //         cancellationToken);
//     //     var series = result?.Results?.FirstOrDefault()?.Series?.FirstOrDefault();
//     //     var record = series?.Rows?.FirstOrDefault();
//     //     if (series == null || record == null)
//     //         return null;

//     //     return new MeasurementLevel
//     //     {
//     //         DevEui = (string)series.GroupedTags["DevEUI"],
//     //         Timestamp = record.Timestamp,
//     //         DistanceMm = record.Distance,
//     //         BatV = record.BatV,
//     //         RssiDbm = record.Rssi
//     //     };
//     // }

//     public async Task<MeasurementDetect[]> Get(string devEui, DateTime? from, DateTime? till,
//         CancellationToken cancellationToken)
//     {
//         (string filterText, object parameters) = GetFilter(devEui, from, till);

//         var query = "SELECT"
//                     + " *"
//                     + " FROM waterlevel"
//                     + filterText
//                     + " GROUP BY *"
//                     + " ORDER BY DESC LIMIT 1000";

//         using var influxClient = new InfluxClient(_options.Endpoint, _options.Username, _options.Password);
//         var result = await influxClient.ReadAsync<RecordLevel>("wateralarm", query, parameters,
//             cancellationToken);

//         var series = result?.Results?.FirstOrDefault()?.Series?.FirstOrDefault();
//         var record = series?.Rows?.Select(record =>
//             new MeasurementLevel
//             {
//                 DevEui = (string)series.GroupedTags["DevEUI"],
//                 Timestamp = record.Timestamp,
//                 DistanceMm = record.Distance,
//                 BatV = record.BatV,
//                 RssiDbm = record.Rssi
//             }).ToArray();

//         return record ?? Array.Empty<MeasurementDetect>();
//     }

//     public async Task<AggregatedMeasurement[]> GetAggregated(string devEui, DateTime? from, DateTime? till, TimeSpan? interval,
//         CancellationToken cancellationToken)
//     {
//         (string filterText, object parameters) = GetFilter(devEui, from, till);

//         string? groupByText;
//         if (interval is {} interval2)
//         {
//             string intervalText;
//             if (interval < TimeSpan.FromHours(1))
//                 intervalText = $"{60 / (60 / (int)interval2.TotalMinutes)}m";
//             else if (interval < TimeSpan.FromDays(1))
//                 intervalText = $"{24 / (24 / (int)interval2.TotalHours)}h";
//             else
//                 intervalText = $"{(int)interval2.TotalDays}d";

//             var timeZoneOffset = (int)TimeZoneInfo.Local.GetUtcOffset(DateTime.UtcNow).TotalSeconds;
//             groupByText = $", time({intervalText}, -{timeZoneOffset}s)";
//         }
//         else
//         {
//             groupByText = null;
//         }

//         var query = "SELECT "
//                     + " MIN(*), MEAN(*), MAX(*), LAST(*)"
//                     + " FROM waterlevel"
//                     + filterText
//                     + " GROUP BY *"
//                     + groupByText
//                     + " ORDER BY DESC LIMIT 1000";

        
//         using var influxClient = new InfluxClient(_options.Endpoint, _options.Username, _options.Password);
//         var result = await influxClient.ReadAsync<RecordLevelAgg>("wateralarm", query, parameters,
//             cancellationToken);

//         var series = result?.Results?.FirstOrDefault()?.Series?.FirstOrDefault();
//         var record = series?.Rows?.Select(record =>
//             new AggregatedMeasurement
//             {
//                 DevEui = (string)series.GroupedTags["DevEUI"],
//                 Timestamp = record.Timestamp,
//                 MinDistanceMm = record.MinDistance,
//                 MeanDistanceMm = record.MeanDistance,
//                 MaxDistanceMm = record.MaxDistance,
//                 LastDistanceMm = record.LastDistance,
//                 BatV = record.BatV,
//                 RssiDbm = record.Rssi
//             }).ToArray();

//         return record ?? Array.Empty<AggregatedMeasurement>();
//     }

//     private (string, object) GetFilter(string devEui, DateTime? from, DateTime? till)
//     {
//         StringBuilder filterText = new();
//         Dictionary<string, object> parameters = new();

//         filterText.Append(" WHERE");
//         filterText.Append(" DevEUI = $devEui");
//         parameters.Add("devEui", devEui);
        
//         if (from.HasValue)
//         {
//             filterText.Append(" AND time >= $from");
//             parameters.Add("from", from.Value);
//         }
        
//         if (till.HasValue)
//         {
//             filterText.Append(" AND time < $till");
//             parameters.Add("till", till.Value);
//         }

//         return (filterText.ToString(), parameters);
//     }

//     public async Task<MeasurementDetect?> GetLastBefore(string devEui, DateTime timestamp,
//         CancellationToken cancellationToken)
//     {
//         using var influxClient = new InfluxClient(_options.Endpoint, _options.Username, _options.Password);
//         var result = await influxClient.ReadAsync<RecordLevel>("wateralarm",
//             $"SELECT * FROM waterlevel WHERE DevEUI = $devEui AND time <= $before GROUP BY * ORDER BY DESC LIMIT 1",
//             new { devEui, before = timestamp },
//             cancellationToken);
//         var series = result?.Results?.FirstOrDefault()?.Series?.FirstOrDefault();
//         var record = series?.Rows?.FirstOrDefault();
//         if (series == null || record == null)
//             return null;

//         return new MeasurementLevel
//         {
//             DevEui = (string)series.GroupedTags["DevEUI"],
//             Timestamp = record.Timestamp,
//             DistanceMm = record.Distance,
//             BatV = record.BatV,
//             RssiDbm = record.Rssi
//         };
//     }

//     public async Task<AggregatedMeasurement?> GetLastMedian(string devEui, DateTime from, CancellationToken cancellationToken)
//     {
//         var result = await GetAggregated(devEui, from, null, null, cancellationToken);
//         if (result.Length > 0)
//             return result[0];
//         return null;
//     }

//     // ReSharper disable UnusedAutoPropertyAccessor.Local

//     private class RecordLevel
//     {
//         [InfluxTimestamp] public DateTime Timestamp { get; set; }

//         [InfluxTag("DevEUI")] public string DevEui { get; set; } = default!;

//         [InfluxField("batV")] public double BatV { get; set; }

//         [InfluxField("distance")] public int Distance { get; set; }

//         [InfluxField("RSSI")] public int Rssi { get; set; }
//     }

//     private class RecordLevelAgg
//     {
//         [InfluxTimestamp] public DateTime Timestamp { get; set; }

//         [InfluxTag("DevEUI")] public string DevEui { get; set; } = default!;

//         [InfluxField("last_batV")] public double BatV { get; set; }

//         [InfluxField("min_distance")] public int? MinDistance { get; set; }
//         [InfluxField("mean_distance")] public int? MeanDistance { get; set; }
//         [InfluxField("max_distance")] public int? MaxDistance { get; set; }
//         [InfluxField("last_distance")] public int? LastDistance { get; set; }

//         [InfluxField("last_RSSI")] public int Rssi { get; set; }
//     }

//     // ReSharper restore UnusedAutoPropertyAccessor.Local
// }