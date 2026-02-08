using System.Globalization;
using System.Text;
using Core.Commands;
using Core.Communication;
using Core.Entities;
using Core.Helpers;
using Core.Queries;
using Core.Util;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Site.Services;
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
        GraphTemperature,
        GraphConductivity,
        GraphStatus,
        GraphSignal,
        GraphReception,
        GraphBattery,
        Diagram,
        Trend,
        Details,
        Settings,
        QrCode
    }

    public enum SaveResultEnum
    {
        None, Saved, NotAuthorized, Error,
        InvalidData
    }

    private readonly IMediator _mediator;
    private readonly IUserInfo _userInfo;
    private readonly ITrendService _trendService;
    private readonly IUrlBuilder _urlBuilder;

    public AccountSensor(IMediator mediator, IUserInfo userInfo, ITrendService trendService, IUrlBuilder urlBuilder)
    {
        _mediator = mediator;
        _userInfo = userInfo;
        _trendService = trendService;
        _urlBuilder = urlBuilder;
    }

    public IMeasurementEx? LastMeasurement { get; private set; }
    //public TrendMeasurementEx? TrendMeasurement1H { get; private set; }
    public TrendMeasurementEx? TrendMeasurement6H { get; private set; }
    public TrendMeasurementEx? TrendMeasurement24H { get; private set; }
    public TrendMeasurementEx? TrendMeasurement7D { get; private set; }
    public TrendMeasurementEx? TrendMeasurement30D { get; private set; }
    public Core.Entities.AccountSensor? AccountSensorEntity { get; private set; }

    public PageTypeEnum PageType { get; private set; }
    public bool Preview { get; private set; }
    public SaveResultEnum SaveResult { get; private set; }
    public int FromDays { get; private set; }

    public string? QrBaseUrl { get; private set; }

    public async Task OnGet(string accountLink, string sensorLink,
        [FromQuery] PageTypeEnum page = PageTypeEnum.GraphDefault,
        [FromQuery] bool preview = false,
        [FromQuery] SaveResultEnum saveResult = SaveResultEnum.None,
        [FromQuery] int fromDays = 21)
    {
        PageType = page;
        Preview = preview;
        SaveResult = saveResult;
        FromDays = fromDays;

        AccountSensorEntity = await _mediator.Send(new AccountSensorByLinkQuery
        {
            SensorLink = sensorLink,
            AccountLink = accountLink
        });

        if (AccountSensorEntity != null)
        {
            QrBaseUrl = _urlBuilder.BuildUrl(AccountSensorEntity.RestPath);

            LastMeasurement = await _mediator.Send(new LastMeasurementQuery
            {
                AccountSensor = AccountSensorEntity
            });

            switch (PageType)
            {
                case PageTypeEnum.Trend:
                    if (LastMeasurement != null)
                    {
                        if (LastMeasurement is MeasurementLevelEx measurementLevelEx)
                        {
                            var trendMeasurements = await _trendService.GetTrendMeasurements(measurementLevelEx,
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
                    }
                    break;
            }
        }
    }

    public async Task<IActionResult> OnPostTestMailAlertAsync(
        [FromServices] IMediator mediator,
        [FromServices] IMessenger messenger,
        [FromRoute] string accountLink,
        [FromRoute] string sensorLink)
    {
        if (!_userInfo.IsAuthenticated())
            return Unauthorized();

        var accountSensorEntity = await mediator.Send(new AccountSensorByLinkQuery
        {
            SensorLink = sensorLink,
            AccountLink = accountLink
        });

        if (accountSensorEntity == null)
            return NotFound();

        if (!await _userInfo.CanUpdateAccountSensor(accountSensorEntity))
            return Forbid();

        await messenger.SendAlertMailAsync(
            accountSensorEntity.Account.Email,
            _urlBuilder.BuildUrl(accountSensorEntity.RestPath),
            accountSensorEntity.Name,
            "Er is gelukkig geen probleem, dit is een test alert.",
            "Test alert");

        return new JsonResult(new { success = true });
    }

    public async Task<IActionResult> OnPostAddAlarmAsync(
        string accountLink,
        string sensorLink,
        [FromForm] string alarmType,
        [FromForm] string? alarmThreshold)
    {
        try
        {
            double? alarmThresholdNumber;
            if (!string.IsNullOrWhiteSpace(alarmThreshold) && double.TryParse(alarmThreshold, NumberStyles.Float, CultureInfo.InvariantCulture, out double alarmThreshold2))
                alarmThresholdNumber = alarmThreshold2;
            else
                alarmThresholdNumber = null;

            var accountSensorEntity = await _mediator.Send(new AccountSensorByLinkQuery
            {
                SensorLink = sensorLink,
                AccountLink = accountLink
            });

            if (accountSensorEntity == null)
                return new JsonResult(new { success = false, message = "Sensor not found" });

            if (!await _userInfo.CanUpdateAccountSensor(accountSensorEntity))
                return new JsonResult(new { success = false, message = "Not authorized" });

            if (!Enum.TryParse<AccountSensorAlarmType>(alarmType, out var parsedAlarmType))
                return new JsonResult(new { success = false, message = "Invalid alarm type" });

            // Validate threshold for alarm types that require it
            if (parsedAlarmType != AccountSensorAlarmType.DetectOn && !alarmThresholdNumber.HasValue)
                return new JsonResult(new { success = false, message = "Threshold is required for this alarm type" });

            await _mediator.Send(new AddAccountSensorAlarmCommand
            {
                AccountId = accountSensorEntity.Account.Uid,
                SensorId = accountSensorEntity.Sensor.Uid,
                AlarmId = Guid.NewGuid(),
                AlarmType = parsedAlarmType,
                AlarmThreshold = alarmThresholdNumber
            });

            return new JsonResult(new { success = true });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, message = ex.Message });
        }
    }

    public async Task<IActionResult> OnPostUpdateAlarmAsync(
        string accountLink,
        string sensorLink,
        [FromForm] string alarmUid,
        [FromForm] string alarmType,
        [FromForm] double? alarmThreshold)
    {
        try
        {
            var accountSensorEntity = await _mediator.Send(new AccountSensorByLinkQuery
            {
                SensorLink = sensorLink,
                AccountLink = accountLink
            });

            if (accountSensorEntity == null)
                return new JsonResult(new { success = false, message = "Sensor not found" });

            if (!await _userInfo.CanUpdateAccountSensor(accountSensorEntity))
                return new JsonResult(new { success = false, message = "Not authorized" });

            if (!Guid.TryParse(alarmUid, out var parsedAlarmUid))
                return new JsonResult(new { success = false, message = "Invalid alarm ID" });

            if (!Enum.TryParse<AccountSensorAlarmType>(alarmType, out var parsedAlarmType))
                return new JsonResult(new { success = false, message = "Invalid alarm type" });

            // Validate threshold for alarm types that require it
            if (parsedAlarmType != AccountSensorAlarmType.DetectOn && !alarmThreshold.HasValue)
                return new JsonResult(new { success = false, message = "Threshold is required for this alarm type" });

            await _mediator.Send(new UpdateAccountSensorAlarmCommand
            {
                AccountUid = accountSensorEntity.Account.Uid,
                SensorUid = accountSensorEntity.Sensor.Uid,
                AlarmUid = parsedAlarmUid,
                AlarmType = new Optional<AccountSensorAlarmType>(true, parsedAlarmType),
                AlarmThreshold = new Optional<double?>(true, alarmThreshold)
            });

            return new JsonResult(new { success = true });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, message = ex.Message });
        }
    }

    public async Task<IActionResult> OnPostDeleteAlarmAsync(
        string accountLink,
        string sensorLink,
        [FromForm] string alarmUid)
    {
        try
        {
            var accountSensorEntity = await _mediator.Send(new AccountSensorByLinkQuery
            {
                SensorLink = sensorLink,
                AccountLink = accountLink
            });

            if (accountSensorEntity == null)
                return new JsonResult(new { success = false, message = "Sensor not found" });

            if (!await _userInfo.CanUpdateAccountSensor(accountSensorEntity))
                return new JsonResult(new { success = false, message = "Not authorized" });

            if (!Guid.TryParse(alarmUid, out var parsedAlarmUid))
                return new JsonResult(new { success = false, message = "Invalid alarm ID" });

            await _mediator.Send(new RemoveAlarmFromAccountSensorCommand
            {
                AccountUid = accountSensorEntity.Account.Uid,
                SensorUid = accountSensorEntity.Sensor.Uid,
                AlarmUid = parsedAlarmUid
            });

            return new JsonResult(new { success = true });
        }
        catch (Exception ex)
        {
            return new JsonResult(new { success = false, message = ex.Message });
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
        for (var safetyCounter = 0; safetyCounter < 10; safetyCounter++)
        {
            var result = await _mediator.Send(new MeasurementsQuery
            {
                AccountSensor = accountSensorEntity,
                From = from,
                Till = last
            });

            if (result == null || result.Length == 0)
                break;

            foreach (var measurementEx in result)
            {
                MeasurementLevelEx? measurementLevelEx = measurementEx as MeasurementLevelEx;

                // TODO: Implement for other measurement types

                StringBuilder sb = new();
                sb
                    .Append('"').Append(measurementEx.DevEui).Append('"')
                    .Append(',')
                    .Append('"').Append(measurementEx.Timestamp.ToString("yyyy-M-d HH:mm:ss")).Append('"')
                    .Append(',')
                    .Append(measurementLevelEx?.Distance.DistanceMm)
                    .Append(',')
                    .Append(measurementEx.BatV.ToString(CultureInfo.InvariantCulture))
                    .Append(',')
                    .Append(measurementEx.RssiDbm.ToString(CultureInfo.InvariantCulture))
                    .Append(',')
                    .Append(measurementLevelEx?.Distance.LevelFraction?.ToString(CultureInfo.InvariantCulture))
                    .Append(',')
                    .Append(measurementLevelEx?.Distance.WaterL?.ToString(CultureInfo.InvariantCulture))
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
        [FromForm] int? order,
        [FromForm] int? distanceMmFull,
        [FromForm] int? distanceMmEmpty,
        [FromForm] int? unusableHeightMm,
        [FromForm] int? capacityL,
        [FromForm] bool? alertsEnabled,
        [FromForm] string? manholeAreaM2)
    {
        SaveResultEnum result =
            await UpdateSettings(
                mediator, accountLink, sensorLink, page, sensorName, order,
                distanceMmFull, distanceMmEmpty, unusableHeightMm, capacityL,
                alertsEnabled, manholeAreaM2);

        return Redirect($"?page={page}&saveResult={result}");
    }

    public async Task<SaveResultEnum> UpdateSettings(
        IMediator mediator,
        string accountLink,
        string sensorLink,
        PageTypeEnum? page,
        string? sensorName,
        int? order,
        int? distanceMmFull,
        int? distanceMmEmpty,
        int? unusableHeightMm,
        int? capacityL,
        bool? alertsEnabled,
        string? manholeAreaM2)
    {
        if (page != PageTypeEnum.Settings)
        {
            return SaveResultEnum.Error;
        }

        if (!_userInfo.IsAuthenticated())
        {
            return SaveResultEnum.NotAuthorized;
        }
        // Basic validation
        else if (sensorName == null
                || capacityL is <= 0
                || distanceMmFull is <= 0
                || distanceMmEmpty is <= 0
                || unusableHeightMm is < 0)
        {
            return SaveResultEnum.InvalidData;
        }

        var accountSensor = await mediator.Send(new AccountSensorByLinkQuery
        {
            SensorLink = sensorLink,
            AccountLink = accountLink
        });

        if (accountSensor == null)
        {
            // AccountSensor not found
            return SaveResultEnum.Error;
        }

        if (!await _userInfo.CanUpdateAccountSensor(accountSensor))
        {
            // Login not allowed to update AccountSensor
            return SaveResultEnum.NotAuthorized;
        }

        // Sensor-specific validation
        if (accountSensor.Sensor.Type == SensorType.Level)
        {
            if (distanceMmFull.HasValue && distanceMmEmpty.HasValue
                && (distanceMmEmpty.Value - (unusableHeightMm ?? 0)) <= distanceMmFull.Value)
            {
                return SaveResultEnum.InvalidData;
            }
        }
        else if (accountSensor.Sensor.Type == SensorType.LevelPressure)
        {
            if (distanceMmFull.HasValue
                && ((unusableHeightMm ?? 0) >= ((distanceMmEmpty ?? 0) + distanceMmFull.Value)))
            {
                return SaveResultEnum.InvalidData;
            }
        }

        double? manholeAreaM2Parsed;
        if (manholeAreaM2 == null)
        {
            manholeAreaM2Parsed = null;
        }
        else
        {
            if (!double.TryParse(manholeAreaM2, CultureInfo.InvariantCulture.NumberFormat, out double manholeAreaM2Parsed2)
                || manholeAreaM2Parsed2 < 0.0)
            {
                return SaveResultEnum.InvalidData;
            }
            manholeAreaM2Parsed = manholeAreaM2Parsed2;
        }

        try
        {
            await mediator.Send(new UpdateAccountSensorCommand
            {
                AccountUid = accountSensor.Account.Uid,
                SensorUid = accountSensor.Sensor.Uid,
                CapacityL = new Optional<int?>(true, capacityL),
                DistanceMmFull = new Optional<int?>(true, distanceMmFull),
                DistanceMmEmpty = new Optional<int?>(true, distanceMmEmpty),
                UnusableHeightMm = new Optional<int?>(true, unusableHeightMm),
                Name = Optional.From(sensorName),
                Order = new Optional<int>(true, order ?? 0),
                AlertsEnabled = new Optional<bool>(true, alertsEnabled ?? false),
                ManholeAreaM2 = new Optional<double?>(true, manholeAreaM2Parsed)
            });

            return SaveResultEnum.Saved;
        }
        catch (Exception)
        {
            return SaveResultEnum.Error;
        }
    }
}