using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;
using Core.Entities;
using Core.Queries;
using MediatR;
using Microsoft.AspNetCore.CookiePolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Site.Pages;

public class MeasurementDistance
{
    private readonly Core.Entities.AccountSensor _accountSensor;

    public MeasurementDistance(int? distanceMm, Core.Entities.AccountSensor accountSensor)
    {
        DistanceMm = distanceMm;
        _accountSensor = accountSensor;
    }

    public int? DistanceMm { get; }

    public double? RealLevelFraction
    {
        get
        {
            if (DistanceMm.HasValue && _accountSensor is { DistanceMmEmpty: not null, DistanceMmFull: not null })
                return ((double)_accountSensor.DistanceMmEmpty.Value - DistanceMm.Value)
                       / ((double)_accountSensor.DistanceMmEmpty.Value - _accountSensor.DistanceMmFull.Value);
            return null;
        }
    }

    public double? LevelFraction
    {
        get
        {
            var realLevelFraction = RealLevelFraction;
            if (!realLevelFraction.HasValue)
                return null;
            if (realLevelFraction.Value > 1.0)
                return 1.0;
            if (realLevelFraction.Value < 0.0)
                return 0.0;
            return realLevelFraction;
        }
    }

    public double? WaterL
    {
        get
        {
            var levelFraction = LevelFraction;
            if (levelFraction != null && _accountSensor.CapacityL.HasValue)
                return levelFraction.Value * _accountSensor.CapacityL.Value;

            return null;
        }
    }
}

public class MeasurementEx
{
    private readonly Core.Entities.AccountSensor _accountSensor;
    private readonly Measurement _measurement;

    public MeasurementEx(Measurement measurement, Core.Entities.AccountSensor accountSensor)
    {
        _measurement = measurement;
        _accountSensor = accountSensor;
    }

    public Core.Entities.AccountSensor AccountSensor => _accountSensor;
    public string DevEui => _measurement.DevEui;
    public DateTime Timestamp => _measurement.Timestamp;
    public MeasurementDistance Distance => new(_measurement.DistanceMm, _accountSensor);
    public double BatV => _measurement.BatV;
    public double RssiDbm => _measurement.RssiDbm;
    public double RssiPrc => (_measurement.RssiDbm + 150.0) / 60.0 * 80.0;
    public double BatteryPrc => (_measurement.BatV - 3.0) / 0.335 * 100.0;
}

public class MeasurementAggEx
{
    private readonly Core.Entities.AccountSensor _accountSensor;
    private readonly AggregatedMeasurement _aggregatedMeasurement;

    public MeasurementAggEx(AggregatedMeasurement aggregatedMeasurement, Core.Entities.AccountSensor accountSensor)
    {
        _aggregatedMeasurement = aggregatedMeasurement;
        _accountSensor = accountSensor;
    }

    public string DevEui => _aggregatedMeasurement.DevEui;
    public DateTime Timestamp => _aggregatedMeasurement.Timestamp;
    public MeasurementDistance MinDistance => new(_aggregatedMeasurement.MinDistanceMm, _accountSensor);
    public MeasurementDistance MeanDistance => new(_aggregatedMeasurement.MeanDistanceMm, _accountSensor);
    public MeasurementDistance MaxDistance => new(_aggregatedMeasurement.MaxDistanceMm, _accountSensor);
    public MeasurementDistance LastDistance => new(_aggregatedMeasurement.LastDistanceMm, _accountSensor);
    public double BatV => _aggregatedMeasurement.BatV;
    public double RssiDbm => _aggregatedMeasurement.RssiDbm;
    public double RssiPrc => (_aggregatedMeasurement.RssiDbm + 150.0) / 60.0 * 80.0;
    public double BatteryPrc => (_aggregatedMeasurement.BatV - 3.0) / 0.335 * 100.0;
}

public class TrendMeasurementEx
{
    private readonly TimeSpan _timeSpan;
    private readonly MeasurementEx _measurementEx;
    private readonly MeasurementEx _trendMeasurementEx;

    public TrendMeasurementEx(TimeSpan timeSpan, Measurement trend, MeasurementEx measurementEx)
    {
        _timeSpan = timeSpan;
        _measurementEx = measurementEx;
        _trendMeasurementEx = new MeasurementEx(trend, measurementEx.AccountSensor);
    }

    public double? DifferenceWaterL => 
        _measurementEx.Distance.WaterL.HasValue && _trendMeasurementEx.Distance.WaterL.HasValue 
            ? _measurementEx.Distance.WaterL.Value - _trendMeasurementEx.Distance.WaterL.Value
            : null;

    public double? DifferenceWaterLPerDay =>
        DifferenceWaterL.HasValue ? DifferenceWaterL.Value / _timeSpan.TotalDays : null;

    public TimeSpan? TimeTillEmpty => 
        _measurementEx.Distance.WaterL.HasValue && _trendMeasurementEx.Distance.WaterL.HasValue 
                                                && _measurementEx.Distance.WaterL.Value < _trendMeasurementEx.Distance.WaterL.Value
            ? _measurementEx.Distance.WaterL.Value / (_trendMeasurementEx.Distance.WaterL.Value - _measurementEx.Distance.WaterL.Value) * _timeSpan
            : null;
    
    public TimeSpan? TimeTillFull => 
        _measurementEx.Distance.WaterL.HasValue && _trendMeasurementEx.Distance.WaterL.HasValue 
                                                && _measurementEx.Distance.WaterL.Value > _trendMeasurementEx.Distance.WaterL.Value
            ? (_measurementEx.AccountSensor.CapacityL - _measurementEx.Distance.WaterL.Value) / (_measurementEx.Distance.WaterL.Value - _trendMeasurementEx.Distance.WaterL.Value) * _timeSpan
            : null;
}

public class AccountSensor : PageModel
{
    public enum PageTypeEnum
    {
        Graph6H,
        Graph24H,
        Graph7D,
        Graph3M,
        Trend,
        Details
    }

    private readonly IMediator _mediator;

    public AccountSensor(IMediator mediator)
    {
        _mediator = mediator;
    }

    public MeasurementEx? LastMeasurement { get; private set; }
    public MeasurementAggEx[]? Measurements { get; private set; }
    public TrendMeasurementEx? TrendMeasurement1H { get; private set; }
    public TrendMeasurementEx? TrendMeasurement6H { get; private set; }
    public TrendMeasurementEx? TrendMeasurement24H { get; private set; }
    public TrendMeasurementEx? TrendMeasurement7D { get; private set; }
    public TrendMeasurementEx? TrendMeasurement30D { get; private set; }
    public Core.Entities.AccountSensor? AccountSensorEntity { get; private set; }

    public PageTypeEnum PageType { get; private set; }

    public async Task OnGet(string accountLink, string sensorLink, [FromQuery] PageTypeEnum page = PageTypeEnum.Graph7D)
    {
        PageType = page;

        AccountSensorEntity = await _mediator.Send(new SensorByLinkQuery
        {
            SensorLink = sensorLink,
            AccountLink = accountLink
        });

        if (AccountSensorEntity != null)
        {
            var lastMeasurement = await _mediator.Send(new LastMeasurementQuery
                { DevEui = AccountSensorEntity.Sensor.DevEui });
            if (lastMeasurement != null) LastMeasurement = new MeasurementEx(lastMeasurement, AccountSensorEntity);

            Tuple<TimeSpan, TimeSpan>? period = null;
            switch (PageType)
            {
                case PageTypeEnum.Graph6H:
                    period = Tuple.Create(TimeSpan.FromHours(6), TimeSpan.FromMinutes(20));
                    break;
                case PageTypeEnum.Graph24H:
                    period = Tuple.Create(TimeSpan.FromDays(1), TimeSpan.FromHours(1));
                    break;
                case PageTypeEnum.Graph7D:
                    period = Tuple.Create(TimeSpan.FromDays(7), TimeSpan.FromHours(6));
                    break;
                case PageTypeEnum.Graph3M:
                    period = Tuple.Create(TimeSpan.FromDays(90), TimeSpan.FromDays(7));
                    break;
                case PageTypeEnum.Trend:
                    if (LastMeasurement != null)
                    {
                        TrendMeasurement1H = await GetTrendMeasurement(TimeSpan.FromHours(1), LastMeasurement);
                        TrendMeasurement6H = await GetTrendMeasurement(TimeSpan.FromHours(6), LastMeasurement);
                        TrendMeasurement24H = await GetTrendMeasurement(TimeSpan.FromHours(24), LastMeasurement);
                        TrendMeasurement7D = await GetTrendMeasurement(TimeSpan.FromDays(7), LastMeasurement);
                        TrendMeasurement30D = await GetTrendMeasurement(TimeSpan.FromDays(30), LastMeasurement);
                    }
                    break;
            }

            if (period != null)
                Measurements = (await _mediator.Send(new AggregatedMeasurementsQuery
                    {
                        DevEui = AccountSensorEntity.Sensor.DevEui,
                        From = DateTime.UtcNow.Add(-period.Item1),
                        Interval = period.Item2
                    }))
                    .OrderBy(m => m.Timestamp)
                    .Select(m => new MeasurementAggEx(m, AccountSensorEntity))
                    .ToArray();
        }
    }

    public async Task<IActionResult> OnGetExportCsv(string accountLink, string sensorLink)
    {
        // https://swimburger.net/blog/dotnet/create-zip-files-on-http-request-without-intermediate-files-using-aspdotnet-mvc-razor-pages-and-endpoints#better-mvc
        
        Response.ContentType = "text/csv";
        Response.Headers.Add("Content-Disposition", "attachment; filename=\"Export.csv\"");

        
        await using TextWriter textWriter = new StreamWriter(Response.BodyWriter.AsStream());
        var accountSensorEntity = await _mediator.Send(new SensorByLinkQuery
        {
            SensorLink = sensorLink,
            AccountLink = accountLink
        });

        if (accountSensorEntity == null)
            return NotFound();

        {
            StringBuilder sb = new();
            sb
                .Append('"').Append("DevEui").Append('"')
                .Append(',')
                .Append('"').Append("Timestamp").Append('"')
                .Append(',')
                .Append('"').Append("DistanceMm").Append('"')
                .Append(',')
                .Append('"').Append("BatV").Append('"')
                .Append(',')
                .Append('"').Append("RssiDbm").Append('"')
                .Append(',')
                .Append('"').Append("LevelPrc").Append('"')
                .Append(',')
                .Append('"').Append("WaterL").Append('"')
                .Append(',')
                .Append('"').Append("BatteryPrc").Append('"')
                .Append(',')
                .Append('"').Append("RssiPrc").Append('"')
                ;
            await textWriter.WriteLineAsync(sb.ToString());
        }

        DateTime from = DateTime.UtcNow.AddYears(-1);
        DateTime? last = null;
        while (true)
        {
            var result = await _mediator.Send(new MeasurementsQuery
            {
                DevEui = accountSensorEntity.Sensor.DevEui,
                From = from,
                Till = last
            });

            if (result.Length == 0)
                break;

            foreach (var record in result)
            {
                MeasurementEx measurementEx = new(record, accountSensorEntity);
                StringBuilder sb = new();
                sb
                    .Append('"').Append(measurementEx.DevEui).Append('"')
                    .Append(',')
                    .Append('"').Append(measurementEx.Timestamp.ToString("yyyy-M-d HH:mm:ss")).Append('"')
                    .Append(',')
                    .Append(measurementEx.Distance.DistanceMm)
                    .Append(',')
                    .Append(measurementEx.BatV.ToString(CultureInfo.InvariantCulture))
                    .Append(',')
                    .Append(measurementEx.RssiDbm.ToString(CultureInfo.InvariantCulture))
                    .Append(',')
                    .Append(measurementEx.Distance.LevelFraction?.ToString(CultureInfo.InvariantCulture))
                    .Append(',')
                    .Append(measurementEx.Distance.WaterL?.ToString(CultureInfo.InvariantCulture))
                    .Append(',')
                    .Append(measurementEx.BatteryPrc.ToString(CultureInfo.InvariantCulture))
                    .Append(',')
                    .Append(measurementEx.RssiPrc.ToString(CultureInfo.InvariantCulture))
                    ;
                await textWriter.WriteLineAsync(sb.ToString());
            }

            last = result[^1].Timestamp;
        }

        return new EmptyResult();
    }

    private async Task<TrendMeasurementEx?> GetTrendMeasurement(TimeSpan timeSpan, MeasurementEx lastMeasurement)
    {
        var trendMeasurement = await _mediator.Send(
            new MeasurementLastBeforeQuery
            {
                DevEui = lastMeasurement.AccountSensor.Sensor.DevEui,
                Timestamp = lastMeasurement.Timestamp.Add(-timeSpan)
            });
        if (trendMeasurement == null)
            return null;
        return new TrendMeasurementEx(timeSpan, trendMeasurement, lastMeasurement);        
    }
}