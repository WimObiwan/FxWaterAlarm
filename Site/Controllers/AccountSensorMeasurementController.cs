using Core.Entities;
using Core.Queries;
using Core.Util;
using MediatR;
using Microsoft.AspNetCore.Mvc;

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
}

[Route("api/a/{AccountLink}/s/{SensorLink}/m")]
public class AccountSensorMeasurementController : Controller
{
    private readonly IMediator _mediator;

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

        DateTime from = DateTime.UtcNow.AddDays(-fromDays);
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
            Data = result?.Reverse() 
        });
    }

    public AccountSensorMeasurementController(IMediator mediator)
    {
        _mediator = mediator;
    }
}