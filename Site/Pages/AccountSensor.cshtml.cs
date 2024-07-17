using System.Globalization;
using System.Text;
using Core.Commands;
using Core.Entities;
using Core.Queries;
using Core.Util;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Site.Utilities;

namespace Site.Pages;

public class AccountSensor : PageModel
{
    public enum PageTypeEnum
    {
        GraphDefault,
        GraphVolume,
        GraphPercentage,
        GraphHeight,
        GraphDistance,
        GraphSignal,
        GraphBattery,
        Trend,
        Details,
        Settings,
        QrCode
    }

    public enum SaveResultEnum { None, Saved, NotAuthorized, Error,
        InvalidData
    }
    
    private readonly IMediator _mediator;
    private readonly IUserInfo _userInfo;
    private readonly ITrendService _trendService;

    public AccountSensor(IMediator mediator, IUserInfo userInfo, ITrendService trendService)
    {
        _mediator = mediator;
        _userInfo = userInfo;
        _trendService = trendService;
    }

    public MeasurementEx? LastMeasurement { get; private set; }
    //public TrendMeasurementEx? TrendMeasurement1H { get; private set; }
    public TrendMeasurementEx? TrendMeasurement6H { get; private set; }
    public TrendMeasurementEx? TrendMeasurement24H { get; private set; }
    public TrendMeasurementEx? TrendMeasurement7D { get; private set; }
    public TrendMeasurementEx? TrendMeasurement30D { get; private set; }
    public Core.Entities.AccountSensor? AccountSensorEntity { get; private set; }

    public PageTypeEnum PageType { get; private set; }
    public bool Preview { get; private set; }
    public SaveResultEnum SaveResult { get; private set; }
    
    public string? QrBaseUrl { get; private set; }

    public async Task OnGet(string accountLink, string sensorLink, 
        [FromQuery] PageTypeEnum page = PageTypeEnum.GraphDefault,
        [FromQuery] bool preview = false,
        [FromQuery] SaveResultEnum saveResult = SaveResultEnum.None)
    {
        PageType = page;
        Preview = preview;
        SaveResult = saveResult;
        QrBaseUrl = $"https://wateralarm.be/a/{accountLink}/s/{sensorLink}";

        AccountSensorEntity = await _mediator.Send(new AccountSensorByLinkQuery
        {
            SensorLink = sensorLink,
            AccountLink = accountLink
        });

        if (AccountSensorEntity != null)
        {
            var lastMeasurement = await _mediator.Send(new LastMeasurementQuery
                { DevEui = AccountSensorEntity.Sensor.DevEui });
            if (lastMeasurement != null) LastMeasurement = new MeasurementEx(lastMeasurement, AccountSensorEntity);

            switch (PageType)
            {
                case PageTypeEnum.Trend:
                    if (LastMeasurement != null)
                    {
                        var trendMeasurements = await _trendService.GetTrendMeasurements(LastMeasurement,
                            //TimeSpan.FromHours(1),
                            TimeSpan.FromHours(6),
                            TimeSpan.FromHours(24),
                            TimeSpan.FromDays(7),
                            TimeSpan.FromDays(30));
                        
                        //TrendMeasurement1H = trendMeasurements[0];
                        TrendMeasurement6H = trendMeasurements[0];
                        TrendMeasurement24H = trendMeasurements[1];
                        TrendMeasurement7D = trendMeasurements[2];
                        TrendMeasurement30D = trendMeasurements[3];
                    }
                    break;
            }
        }
    }

    public async Task<IActionResult> OnGetExportCsv(string accountLink, string sensorLink)
    {
        // https://swimburger.net/blog/dotnet/create-zip-files-on-http-request-without-intermediate-files-using-aspdotnet-mvc-razor-pages-and-endpoints#better-mvc
        
        Response.ContentType = "text/csv";
        Response.Headers.Append("Content-Disposition", "attachment; filename=\"Export.csv\"");

        
        await using TextWriter textWriter = new StreamWriter(Response.BodyWriter.AsStream());
        var accountSensorEntity = await _mediator.Send(new AccountSensorByLinkQuery
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

    public async Task<IActionResult> OnPost(
        [FromServices] IMediator mediator,
        [FromRoute] string accountLink,
        [FromRoute] string sensorLink,
        [FromQuery] PageTypeEnum? page,
        [FromForm] string? sensorName,
        [FromForm] int? distanceMmFull,
        [FromForm] int? distanceMmEmpty,
        [FromForm] int? capacityL,
        [FromForm] bool? alertsEnabled,
        [FromForm] bool? noMinMaxConstraints)
    {
        SaveResultEnum result = SaveResultEnum.Error;
        if (page == PageTypeEnum.Settings)
        {
            if (!_userInfo.IsAuthenticated())
            {
                result = SaveResultEnum.NotAuthorized;
            }
            else if (sensorName == null 
                     || capacityL is <= 0
                     || distanceMmFull is <= 0
                     || distanceMmEmpty is <= 0
                     || distanceMmFull.HasValue && distanceMmEmpty.HasValue && distanceMmEmpty.Value <= distanceMmFull.Value)
            {
                result = SaveResultEnum.InvalidData;
            }
            else
            {
                var accountSensor = await mediator.Send(new AccountSensorByLinkQuery
                {
                    SensorLink = sensorLink,
                    AccountLink = accountLink
                });

                if (accountSensor == null)
                {
                    // AccountSensor not found
                    result = SaveResultEnum.Error;
                }
                else if (!_userInfo.CanUpdateAccountSensor(accountSensor))
                {
                    // Login not allowed to update AccountSensor
                    result = SaveResultEnum.NotAuthorized;
                }
                else
                {
                    try
                    {
                        await mediator.Send(new UpdateAccountSensorCommand
                        {
                            AccountUid = accountSensor.Account.Uid,
                            SensorUid = accountSensor.Sensor.Uid,
                            CapacityL = new Optional<int?>(true, capacityL),
                            DistanceMmFull = new Optional<int?>(true, distanceMmFull),
                            DistanceMmEmpty = new Optional<int?>(true, distanceMmEmpty),
                            Name = Optional.From(sensorName),
                            AlertsEnabled = new Optional<bool>(true, alertsEnabled ?? false),
                            NoMinMaxConstraints = new Optional<bool>(true, noMinMaxConstraints ?? false)
                        });
                        result = SaveResultEnum.Saved;
                    }
                    catch (Exception)
                    {
                        result = SaveResultEnum.Error;
                    }
                }
            }
        }

        return Redirect($"?page={page}&saveResult={result}");
    }
}