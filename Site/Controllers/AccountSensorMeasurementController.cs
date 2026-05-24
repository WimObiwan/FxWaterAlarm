using Core.Audit;
using Core.Commands;
using Core.Entities;
using Core.Queries;
using Core.Util;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Site.Utilities;

namespace Site.Controllers;


public class MeasurementResultDataItem
{
    public required DateTime TimeStamp { get; init; }
    public double? Value { get; init; }
}

public class MeasurementResult
{
    public string? Unit { get; init; }
    public IEnumerable<MeasurementResultDataItem>? Data { get; init; }
    public double? MinValue { get; init; }
    public double? MaxValue { get; init; }
}

internal readonly record struct MeasurementAxisBounds(double MinValue, double MaxValue);

[Route("api/a/{AccountLink}/s/{SensorLink}/m")]
public class AccountSensorMeasurementController : Controller
{
    private const double MinimumLevelAxisSpanMm = 300.0;

    private readonly IMediator _mediator;
    private readonly IAuditService _auditService;

    public async Task<IActionResult> Index(
        string accountLink, string sensorLink, 
        [FromQuery]int fromDays = 7, [FromQuery]GraphType graphType = GraphType.None)
    {
        if (fromDays > 365) fromDays = 365;

        var accountSensor = await _mediator.Send(new AccountSensorByLinkQuery()
        {
            AccountLink = accountLink,
            SensorLink = sensorLink
        });

        if (accountSensor == null)
            return NotFound();

        if (graphType == GraphType.None)
            graphType = accountSensor.GraphType;

        if (graphType == GraphType.None)
            return NoContent();

        DateTime originalFrom = DateTime.UtcNow.AddDays(-fromDays);
        DateTime from = originalFrom;
        if (graphType == GraphType.Reception)
        {
            from = from.AddHours(-24);
        }

        DateTime? last = null;
        IEnumerable<MeasurementResultDataItem>? result = null;
        for (int i = 0; i < 3; i++)
        {
            var measurements = await _mediator.Send(new MeasurementsQuery
                {
                    AccountSensor = accountSensor,
                    From = from,
                    Till = last
                });

            if (measurements == null || measurements.Length == 0)
                break;

            last = measurements[^1].Timestamp;

            var result2 = measurements.Select(measurementEx =>
            {
                double? value;
                switch (graphType)
                {
                    case GraphType.Height:
                    {
                        if (measurementEx is MeasurementLevelEx measurementLevelEx)
                            value = measurementLevelEx.Distance.HeightMm;
                        else
                            value = null;
                        break;
                    }
                    case GraphType.Percentage:
                    {
                        if (measurementEx is MeasurementLevelEx measurementLevelEx)
                            if (measurementLevelEx.Distance.LevelFraction is {} levelFraction)
                                value = Math.Round(levelFraction * 100.0, 2);
                            else
                                value = null;
                        else if (measurementEx is MeasurementMoistureEx measurementMoistureEx)
                            value = measurementMoistureEx.SoilMoisturePrc;
                        else
                            value = null;
                        break;
                    }
                    case GraphType.Volume:
                    {
                        if (measurementEx is MeasurementLevelEx measurementLevelEx)
                            if (measurementLevelEx.Distance.WaterL is {} waterL)
                                value = Math.Round(waterL, 2);
                            else
                                value = null;
                        else
                            value = null;
                        break;
                    }
                    case GraphType.Distance:
                    {
                        if (measurementEx is MeasurementLevelEx measurementLevelEx)
                            value = measurementLevelEx.Distance.DistanceMm;
                        else
                            value = null;
                        break;
                    }
                    case GraphType.Temperature:
                    {
                        if (measurementEx is MeasurementMoistureEx measurementMoistureEx)
                            value = measurementMoistureEx.SoilTemperatureC;
                        else if (measurementEx is MeasurementThermometerEx measurementThermometerEx)
                            value = measurementThermometerEx.TempC;
                        else
                            value = null;
                        break;
                    }
                    case GraphType.Conductivity:
                    {
                        if (measurementEx is MeasurementMoistureEx measurementMoistureEx)
                            value = measurementMoistureEx.SoilConductivity;
                        else
                            value = null;
                        break;
                    }
                    case GraphType.Status:
                    {
                        if (measurementEx is MeasurementDetectEx measurementMoistureEx)
                            value = measurementMoistureEx.Status;
                        else
                            value = null;
                        break;
                    }
                    case GraphType.RssiDbm:
                    {
                        value = measurementEx.RssiDbm;
                        break;
                    }
                    case GraphType.Reception:
                    {
                        value = 1;
                        break;
                    }
                    case GraphType.BatV:
                    {
                        value = measurementEx.BatV;
                        break;
                    }
                    default:
                    {
                        value = null;
                        break;
                    }
                }
                return new MeasurementResultDataItem
                {
                    TimeStamp = measurementEx.Timestamp,
                    Value = value
                };
            });

            if (result == null)
                result = result2;
            else
                result = result.Concat(result2);
        }

        if (graphType == GraphType.Reception)
        {
            result = RollupCountPerHour(originalFrom, result);
        }

        var axisBounds = CalculateAxisBounds(accountSensor, graphType, result);

        string? unit;
        switch (graphType)
        {
            case GraphType.Height:
            case GraphType.Distance:
                unit = "mm";
                break;
            case GraphType.Percentage:
                unit = "%";
                break;
            case GraphType.Volume:
                unit = "l";
                break;
            case GraphType.Temperature:
                unit = "°C";
                break;
            case GraphType.Conductivity:
                unit = "µS/cm";
                break;
            case GraphType.Status:
                unit = "";
                break;
            case GraphType.RssiDbm:
                unit = "dBm";
                break;
            case GraphType.Reception:
                unit = "";
                break;
            case GraphType.BatV:
                unit = "V";
                break;
            default:
                unit = null;
                break;
        }

        return Ok(new MeasurementResult
        {
            Unit = unit,
            Data = result?.Reverse(),
            MinValue = axisBounds?.MinValue,
            MaxValue = axisBounds?.MaxValue
        });
    }

    private static MeasurementAxisBounds? CalculateAxisBounds(
        AccountSensor accountSensor,
        GraphType graphType,
        IEnumerable<MeasurementResultDataItem>? measurementResults)
    {
        if (measurementResults == null)
            return null;

        var minimumSpan = GetMinimumAxisSpan(accountSensor, graphType);
        if (!minimumSpan.HasValue || minimumSpan.Value <= 0.0)
            return null;

        var values = measurementResults
            .Where(item => item.Value.HasValue)
            .Select(item => item.Value!.Value)
            .ToList();

        if (values.Count == 0)
            return null;

        var minValue = values.Min();
        var maxValue = values.Max();
        var span = minimumSpan.Value;

        if (maxValue - minValue < span)
        {
            var midpoint = (maxValue + minValue) / 2.0;
            maxValue = Math.Round(midpoint + span / 2.0, 2);
            minValue = Math.Round(midpoint - span / 2.0, 2);
        }

        var (absoluteMin, absoluteMax) = GetAbsoluteAxisBounds(accountSensor, graphType);

        if (absoluteMin.HasValue && minValue < absoluteMin.Value)
        {
            minValue = absoluteMin.Value;
            if (minValue + span > maxValue)
                maxValue = minValue + span;
        }

        if (absoluteMax.HasValue && maxValue > absoluteMax.Value)
        {
            maxValue = absoluteMax.Value;
            if (maxValue - span < minValue)
                minValue = maxValue - span;
        }

        return new MeasurementAxisBounds(minValue, maxValue);
    }

    private static double? GetMinimumAxisSpan(AccountSensor accountSensor, GraphType graphType)
    {
        if (accountSensor.Sensor.Type is not (SensorType.Level or SensorType.LevelPressure))
            return null;

        return graphType switch
        {
            GraphType.Height => MinimumLevelAxisSpanMm,
            GraphType.Distance when accountSensor.Sensor.Type == SensorType.Level => MinimumLevelAxisSpanMm,
            GraphType.Volume => accountSensor.ResolutionL.HasValue
                ? accountSensor.ResolutionL.Value * MinimumLevelAxisSpanMm
                : null,
            GraphType.Percentage => GetUsableHeightMm(accountSensor) is { } usableHeightMm && usableHeightMm > 0.0
                ? MinimumLevelAxisSpanMm / usableHeightMm * 100.0
                : null,
            _ => null
        };
    }

    private static (double? MinValue, double? MaxValue) GetAbsoluteAxisBounds(AccountSensor accountSensor, GraphType graphType)
    {
        if (accountSensor.NoMinMaxConstraints)
            return (null, null);

        return graphType switch
        {
            GraphType.Height => (0.0, null),
            GraphType.Distance when accountSensor.Sensor.Type == SensorType.Level => (0.0, null),
            GraphType.Percentage => (0.0, 100.0),
            GraphType.Volume => (0.0, accountSensor.UsableCapacityL ?? accountSensor.CapacityL),
            _ => (null, null)
        };
    }

    private static double? GetUsableHeightMm(AccountSensor accountSensor)
    {
        return accountSensor.Sensor.Type switch
        {
            SensorType.Level when accountSensor.DistanceMmEmpty.HasValue && accountSensor.DistanceMmFull.HasValue
                => accountSensor.DistanceMmEmpty.Value - accountSensor.DistanceMmFull.Value - (accountSensor.UnusableHeightMm ?? 0),
            SensorType.LevelPressure when accountSensor.DistanceMmFull.HasValue
                => (accountSensor.DistanceMmEmpty ?? 0) + accountSensor.DistanceMmFull.Value - (accountSensor.UnusableHeightMm ?? 0),
            _ => null
        };
    }

    private IEnumerable<MeasurementResultDataItem>? RollupCountPerHour(DateTime start, IEnumerable<MeasurementResultDataItem>? measurementResults)
    {
        if (measurementResults == null || !measurementResults.Any())
            return measurementResults;

        var data = measurementResults
            .Where(d => d.Value.HasValue)
            .OrderBy(d => d.TimeStamp)
            .ToList();

        //var start = data.First().TimeStamp.Date.AddHours(data.First().TimeStamp.Hour);
        //var end = data.Last().TimeStamp.Date.AddHours(data.Last().TimeStamp.Hour);
        start = start.Date.AddHours(start.Hour);
        var now = DateTime.UtcNow;
        var end = now.Date.AddHours(now.Hour);

        var result = new List<MeasurementResultDataItem>();

        for (var currentHour = start; currentHour <= end; currentHour = currentHour.AddHours(1))
        {
            var from = currentHour.AddHours(-24);
            var to = currentHour;

            var count = data.Count(d => d.TimeStamp >= from && d.TimeStamp < to);
            result.Add(new MeasurementResultDataItem
            {
                TimeStamp = currentHour,
                Value = count
            });
        }

        return result;
    }

    [HttpDelete]
    public async Task<IActionResult> Delete(
        string accountLink, string sensorLink,
        [FromQuery] DateTime timestamp)
    {
        using var actionScope = _auditService.BeginAction("Measurement.Delete", new AuditTarget
        {
            AccountLink = accountLink,
            SensorLink = sensorLink
        });
        await _auditService.LogAsync(AuditOutcome.Attempted);

        var userInfo = HttpContext.RequestServices.GetRequiredService<IUserInfo>();

        try
        {
            var accountSensor = await _mediator.Send(new AccountSensorByLinkQuery()
            {
                AccountLink = accountLink,
                SensorLink = sensorLink
            });

            if (accountSensor == null)
            {
                await _auditService.LogAsync(AuditOutcome.Failed, new AuditDetails { Reason = "Account sensor not found" });
                return NotFound(new { success = false, message = "Account sensor not found" });
            }

            if (!await userInfo.CanUpdateAccountSensor(accountSensor))
            {
                await _auditService.LogAsync(AuditOutcome.Denied, new AuditDetails { Reason = "Not authorized" });
                return Forbid();
            }

            await _mediator.Send(new RemoveMeasurementCommand
            {
                SensorUid = accountSensor.Sensor.Uid,
                Timestamp = timestamp
            });

            await _auditService.LogAsync(AuditOutcome.Succeeded, target: new AuditTarget
            {
                AccountUid = accountSensor.Account.Uid,
                SensorUid = accountSensor.Sensor.Uid,
                AccountLink = accountLink,
                SensorLink = sensorLink,
                DevEui = accountSensor.Sensor.DevEui
            });

            return Ok(new { success = true, message = "Measurement deleted successfully" });
        }
        catch (InvalidOperationException ex)
        {
            await _auditService.LogAsync(AuditOutcome.Failed, new AuditDetails
            {
                Reason = "Invalid operation",
                ExceptionType = ex.GetType().Name,
                Message = ex.Message
            });
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception)
        {
            await _auditService.LogAsync(AuditOutcome.Failed, new AuditDetails { Reason = "Unexpected exception" });
            return StatusCode(500, new { success = false, message = "An error occurred while deleting the measurement" });
        }
    }

    public AccountSensorMeasurementController(IMediator mediator, IAuditService auditService)
    {
        _mediator = mediator;
        _auditService = auditService;
    }
}