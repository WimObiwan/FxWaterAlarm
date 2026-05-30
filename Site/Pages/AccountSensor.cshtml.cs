using Core.Audit;
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
    public sealed class TableMeasurementRow
    {
        public required IMeasurementEx Measurement { get; init; }
        public required MeasurementLevelEx? MeasurementLevel { get; init; }
    }

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
        Table,
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
    private readonly IAuditService _auditService;

    public AccountSensor(IMediator mediator, IUserInfo userInfo, ITrendService trendService, IUrlBuilder urlBuilder, IAuditService auditService)
    {
        _mediator = mediator;
        _userInfo = userInfo;
        _trendService = trendService;
        _urlBuilder = urlBuilder;
        _auditService = auditService;
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
    public int TablePage { get; private set; }
    public IReadOnlyList<TableMeasurementRow> TableMeasurements { get; private set; } = Array.Empty<TableMeasurementRow>();
    public bool TableHasPreviousPage => TablePage > 1;
    public bool TableHasNextPage { get; private set; }
    public bool ShowMeasurementDeleteIcon { get; private set; }

    public string? QrBaseUrl { get; private set; }

    public async Task OnGet(string accountLink, string sensorLink,
        [FromQuery] PageTypeEnum page = PageTypeEnum.GraphDefault,
        [FromQuery] bool preview = false,
        [FromQuery] SaveResultEnum saveResult = SaveResultEnum.None,
        [FromQuery] int fromDays = 21,
        [FromQuery] int tablePage = 1)
    {
        PageType = page;
        Preview = preview;
        SaveResult = saveResult;
        FromDays = fromDays;
        TablePage = tablePage < 1 ? 1 : tablePage;

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
                case PageTypeEnum.Table:
                    ShowMeasurementDeleteIcon = _userInfo.IsAuthenticated()
                                                && await _userInfo.CanUpdateAccountSensor(AccountSensorEntity);
                    await LoadTableMeasurements(AccountSensorEntity, TablePage);
                    break;
            }
        }
    }

    private async Task LoadTableMeasurements(Core.Entities.AccountSensor accountSensorEntity, int tablePage)
    {
        const int pageSize = 100;
        int skip = (tablePage - 1) * pageSize;
        int consumedRows = 0;
        int collectedRows = 0;
        DateTime from = DateTime.UtcNow.AddYears(-1);
        DateTime? till = null;
        List<TableMeasurementRow> rows = new(pageSize);

        for (int safetyCounter = 0; safetyCounter < 100; safetyCounter++)
        {
            var result = await _mediator.Send(new MeasurementsQuery
            {
                AccountSensor = accountSensorEntity,
                From = from,
                Till = till
            });

            if (result == null || result.Length == 0)
                break;

            foreach (var measurement in result)
            {
                if (consumedRows < skip)
                {
                    consumedRows++;
                    continue;
                }

                if (collectedRows < pageSize)
                {
                    rows.Add(new TableMeasurementRow
                    {
                        Measurement = measurement,
                        MeasurementLevel = measurement as MeasurementLevelEx
                    });
                    collectedRows++;
                }
                else
                {
                    TableHasNextPage = true;
                    TableMeasurements = rows;
                    return;
                }
            }

            till = result[^1].Timestamp;
        }

        TableMeasurements = rows;
    }

    public async Task<IActionResult> OnPostTestMailAlertAsync(
        [FromServices] IMediator mediator,
        [FromServices] IMessenger messenger,
        [FromRoute] string accountLink,
        [FromRoute] string sensorLink)
    {
        using var actionScope = _auditService.BeginAction("AccountSensor.TestMailAlert", new AuditTarget
        {
            AccountLink = accountLink,
            SensorLink = sensorLink
        });
        await _auditService.LogAsync(AuditOutcome.Attempted);

        if (!_userInfo.IsAuthenticated())
        {
            await _auditService.LogAsync(AuditOutcome.Denied, new AuditDetails { Reason = "User is not authenticated" });
            return Unauthorized();
        }

        var accountSensorEntity = await mediator.Send(new AccountSensorByLinkQuery
        {
            SensorLink = sensorLink,
            AccountLink = accountLink
        });

        if (accountSensorEntity == null)
        {
            await _auditService.LogAsync(AuditOutcome.Failed, new AuditDetails { Reason = "Sensor not found" });
            return NotFound();
        }

        if (!await _userInfo.CanUpdateAccountSensor(accountSensorEntity))
        {
            await _auditService.LogAsync(AuditOutcome.Denied, new AuditDetails { Reason = "Not authorized" },
                target: new AuditTarget
                {
                    AccountUid = accountSensorEntity.Account.Uid,
                    SensorUid = accountSensorEntity.Sensor.Uid,
                    AccountLink = accountLink,
                    SensorLink = sensorLink
                });
            return Forbid();
        }

        await messenger.SendAlertMailAsync(
            accountSensorEntity.Account.Email,
            _urlBuilder.BuildUrl(accountSensorEntity.RestPath),
            accountSensorEntity.Name,
            "Er is gelukkig geen probleem, dit is een test alert.",
            "Test alert");

        await _auditService.LogAsync(AuditOutcome.Succeeded, target: new AuditTarget
        {
            AccountUid = accountSensorEntity.Account.Uid,
            SensorUid = accountSensorEntity.Sensor.Uid,
            AccountLink = accountLink,
            SensorLink = sensorLink
        });

        return new JsonResult(new { success = true });
    }

    public async Task<IActionResult> OnPostAddAlarmAsync(
        string accountLink,
        string sensorLink,
        [FromForm] string alarmType,
        [FromForm] string? alarmThreshold)
    {
        using var actionScope = _auditService.BeginAction("AccountSensorAlarm.Add", new AuditTarget
        {
            AccountLink = accountLink,
            SensorLink = sensorLink
        });
        await _auditService.LogAsync(AuditOutcome.Attempted);

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
            {
                await _auditService.LogAsync(AuditOutcome.Failed, new AuditDetails { Reason = "Sensor not found" });
                return new JsonResult(new { success = false, message = "Sensor not found" });
            }

            if (!await _userInfo.CanUpdateAccountSensor(accountSensorEntity))
            {
                await _auditService.LogAsync(AuditOutcome.Denied, new AuditDetails { Reason = "Not authorized" },
                    target: new AuditTarget { AccountUid = accountSensorEntity.Account.Uid, SensorUid = accountSensorEntity.Sensor.Uid, AccountLink = accountLink, SensorLink = sensorLink });
                return new JsonResult(new { success = false, message = "Not authorized" });
            }

            if (!Enum.TryParse<AccountSensorAlarmType>(alarmType, out var parsedAlarmType))
            {
                await _auditService.LogAsync(AuditOutcome.Failed, new AuditDetails { Reason = "Invalid alarm type" },
                    target: new AuditTarget { AccountUid = accountSensorEntity.Account.Uid, SensorUid = accountSensorEntity.Sensor.Uid, AccountLink = accountLink, SensorLink = sensorLink });
                return new JsonResult(new { success = false, message = "Invalid alarm type" });
            }

            // Validate threshold for alarm types that require it
            if (parsedAlarmType != AccountSensorAlarmType.DetectOn && !alarmThresholdNumber.HasValue)
            {
                await _auditService.LogAsync(AuditOutcome.Failed, new AuditDetails { Reason = "Threshold is required for this alarm type" },
                    target: new AuditTarget { AccountUid = accountSensorEntity.Account.Uid, SensorUid = accountSensorEntity.Sensor.Uid, AccountLink = accountLink, SensorLink = sensorLink });
                return new JsonResult(new { success = false, message = "Threshold is required for this alarm type" });
            }

            await _mediator.Send(new AddAccountSensorAlarmCommand
            {
                AccountId = accountSensorEntity.Account.Uid,
                SensorId = accountSensorEntity.Sensor.Uid,
                AlarmId = Guid.NewGuid(),
                AlarmType = parsedAlarmType,
                AlarmThreshold = alarmThresholdNumber
            });

            await _auditService.LogAsync(AuditOutcome.Succeeded, target: new AuditTarget
            {
                AccountUid = accountSensorEntity.Account.Uid,
                SensorUid = accountSensorEntity.Sensor.Uid,
                AccountLink = accountLink,
                SensorLink = sensorLink
            });

            return new JsonResult(new { success = true });
        }
        catch (Exception ex)
        {
            await _auditService.LogAsync(AuditOutcome.Failed, new AuditDetails
            {
                Reason = "Unexpected exception",
                ExceptionType = ex.GetType().Name,
                Message = ex.Message
            });
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
        using var actionScope = _auditService.BeginAction("AccountSensorAlarm.Update", new AuditTarget
        {
            AccountLink = accountLink,
            SensorLink = sensorLink
        });
        await _auditService.LogAsync(AuditOutcome.Attempted);

        try
        {
            var accountSensorEntity = await _mediator.Send(new AccountSensorByLinkQuery
            {
                SensorLink = sensorLink,
                AccountLink = accountLink
            });

            if (accountSensorEntity == null)
            {
                await _auditService.LogAsync(AuditOutcome.Failed, new AuditDetails { Reason = "Sensor not found" });
                return new JsonResult(new { success = false, message = "Sensor not found" });
            }

            if (!await _userInfo.CanUpdateAccountSensor(accountSensorEntity))
            {
                await _auditService.LogAsync(AuditOutcome.Denied, new AuditDetails { Reason = "Not authorized" },
                    target: new AuditTarget { AccountUid = accountSensorEntity.Account.Uid, SensorUid = accountSensorEntity.Sensor.Uid, AccountLink = accountLink, SensorLink = sensorLink });
                return new JsonResult(new { success = false, message = "Not authorized" });
            }

            if (!Guid.TryParse(alarmUid, out var parsedAlarmUid))
            {
                await _auditService.LogAsync(AuditOutcome.Failed, new AuditDetails { Reason = "Invalid alarm ID" },
                    target: new AuditTarget { AccountUid = accountSensorEntity.Account.Uid, SensorUid = accountSensorEntity.Sensor.Uid, AccountLink = accountLink, SensorLink = sensorLink });
                return new JsonResult(new { success = false, message = "Invalid alarm ID" });
            }

            if (!Enum.TryParse<AccountSensorAlarmType>(alarmType, out var parsedAlarmType))
            {
                await _auditService.LogAsync(AuditOutcome.Failed, new AuditDetails { Reason = "Invalid alarm type" },
                    target: new AuditTarget { AccountUid = accountSensorEntity.Account.Uid, SensorUid = accountSensorEntity.Sensor.Uid, AccountLink = accountLink, SensorLink = sensorLink });
                return new JsonResult(new { success = false, message = "Invalid alarm type" });
            }

            // Validate threshold for alarm types that require it
            if (parsedAlarmType != AccountSensorAlarmType.DetectOn && !alarmThreshold.HasValue)
            {
                await _auditService.LogAsync(AuditOutcome.Failed, new AuditDetails { Reason = "Threshold is required for this alarm type" },
                    target: new AuditTarget { AccountUid = accountSensorEntity.Account.Uid, SensorUid = accountSensorEntity.Sensor.Uid, AccountLink = accountLink, SensorLink = sensorLink });
                return new JsonResult(new { success = false, message = "Threshold is required for this alarm type" });
            }

            await _mediator.Send(new UpdateAccountSensorAlarmCommand
            {
                AccountUid = accountSensorEntity.Account.Uid,
                SensorUid = accountSensorEntity.Sensor.Uid,
                AlarmUid = parsedAlarmUid,
                AlarmType = new Optional<AccountSensorAlarmType>(true, parsedAlarmType),
                AlarmThreshold = new Optional<double?>(true, alarmThreshold)
            });

            await _auditService.LogAsync(AuditOutcome.Succeeded, target: new AuditTarget
            {
                AccountUid = accountSensorEntity.Account.Uid,
                SensorUid = accountSensorEntity.Sensor.Uid,
                AccountLink = accountLink,
                SensorLink = sensorLink
            });

            return new JsonResult(new { success = true });
        }
        catch (Exception ex)
        {
            await _auditService.LogAsync(AuditOutcome.Failed, new AuditDetails
            {
                Reason = "Unexpected exception",
                ExceptionType = ex.GetType().Name,
                Message = ex.Message
            });
            return new JsonResult(new { success = false, message = ex.Message });
        }
    }

    public async Task<IActionResult> OnPostDeleteAlarmAsync(
        string accountLink,
        string sensorLink,
        [FromForm] string alarmUid)
    {
        using var actionScope = _auditService.BeginAction("AccountSensorAlarm.Delete", new AuditTarget
        {
            AccountLink = accountLink,
            SensorLink = sensorLink
        });
        await _auditService.LogAsync(AuditOutcome.Attempted);

        try
        {
            var accountSensorEntity = await _mediator.Send(new AccountSensorByLinkQuery
            {
                SensorLink = sensorLink,
                AccountLink = accountLink
            });

            if (accountSensorEntity == null)
            {
                await _auditService.LogAsync(AuditOutcome.Failed, new AuditDetails { Reason = "Sensor not found" });
                return new JsonResult(new { success = false, message = "Sensor not found" });
            }

            if (!await _userInfo.CanUpdateAccountSensor(accountSensorEntity))
            {
                await _auditService.LogAsync(AuditOutcome.Denied, new AuditDetails { Reason = "Not authorized" },
                    target: new AuditTarget { AccountUid = accountSensorEntity.Account.Uid, SensorUid = accountSensorEntity.Sensor.Uid, AccountLink = accountLink, SensorLink = sensorLink });
                return new JsonResult(new { success = false, message = "Not authorized" });
            }

            if (!Guid.TryParse(alarmUid, out var parsedAlarmUid))
            {
                await _auditService.LogAsync(AuditOutcome.Failed, new AuditDetails { Reason = "Invalid alarm ID" },
                    target: new AuditTarget { AccountUid = accountSensorEntity.Account.Uid, SensorUid = accountSensorEntity.Sensor.Uid, AccountLink = accountLink, SensorLink = sensorLink });
                return new JsonResult(new { success = false, message = "Invalid alarm ID" });
            }

            await _mediator.Send(new RemoveAlarmFromAccountSensorCommand
            {
                AccountUid = accountSensorEntity.Account.Uid,
                SensorUid = accountSensorEntity.Sensor.Uid,
                AlarmUid = parsedAlarmUid
            });

            await _auditService.LogAsync(AuditOutcome.Succeeded, target: new AuditTarget
            {
                AccountUid = accountSensorEntity.Account.Uid,
                SensorUid = accountSensorEntity.Sensor.Uid,
                AccountLink = accountLink,
                SensorLink = sensorLink
            });

            return new JsonResult(new { success = true });
        }
        catch (Exception ex)
        {
            await _auditService.LogAsync(AuditOutcome.Failed, new AuditDetails
            {
                Reason = "Unexpected exception",
                ExceptionType = ex.GetType().Name,
                Message = ex.Message
            });
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
        [FromForm] string? manholeAreaM2,
        [FromForm] string? densityKgPerM3 = null,
        [FromForm] TankGeometry? geometry = null)
    {
        using var actionScope = _auditService.BeginAction("AccountSensor.UpdateSettings", new AuditTarget
        {
            AccountLink = accountLink,
            SensorLink = sensorLink
        });
        await _auditService.LogAsync(AuditOutcome.Attempted);

        SaveResultEnum result =
            await UpdateSettings(
                mediator, accountLink, sensorLink, page, sensorName, order,
                distanceMmFull, distanceMmEmpty, unusableHeightMm, capacityL,
                alertsEnabled, manholeAreaM2, densityKgPerM3, geometry);

        if (result == SaveResultEnum.Saved)
        {
            await _auditService.LogAsync(AuditOutcome.Succeeded);
        }
        else if (result == SaveResultEnum.NotAuthorized)
        {
            await _auditService.LogAsync(AuditOutcome.Denied, new AuditDetails { Reason = "Not authorized" });
        }
        else
        {
            await _auditService.LogAsync(AuditOutcome.Failed, new AuditDetails { Reason = result.ToString() });
        }

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
        string? manholeAreaM2,
        string? densityKgPerM3 = null,
        TankGeometry? geometry = null)
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

        double? densityKgPerM3Parsed;
        if (densityKgPerM3 == null || densityKgPerM3.Trim().Length == 0)
        {
            densityKgPerM3Parsed = null;
        }
        else
        {
            if (!double.TryParse(densityKgPerM3, CultureInfo.InvariantCulture.NumberFormat, out double densityKgPerM3Parsed2)
                || densityKgPerM3Parsed2 <= 0.0)
            {
                return SaveResultEnum.InvalidData;
            }
            densityKgPerM3Parsed = densityKgPerM3Parsed2;
        }

        var geometryParsed = geometry ?? TankGeometry.Default;

        if (densityKgPerM3Parsed.HasValue && accountSensor.Sensor.Type != SensorType.LevelPressure)
            return SaveResultEnum.InvalidData;

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
                ManholeAreaM2 = new Optional<double?>(true, manholeAreaM2Parsed),
                DensityKgPerM3 = new Optional<double?>(true, densityKgPerM3Parsed),
                Geometry = new Optional<TankGeometry>(true, geometryParsed),
            });

            return SaveResultEnum.Saved;
        }
        catch (Exception)
        {
            return SaveResultEnum.Error;
        }
    }
}