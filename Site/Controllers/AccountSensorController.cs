using Core.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Site.Pages;

namespace Site.Controllers;

class AccountSensorDto
{
    public required string? Name { get; init; }
    public required int? CapacityL { get; init; }
    public required double? ResolutionL { get; init; }
    public required int? DistanceMmEmpty { get; init; }
    public required int? DistanceMmFull { get; init; }
    public required DateTime CreateTimestamp { get; init; }
}

class LastMeasurementDto
{
    public DateTime TimeStamp { get; set; }
    public double BatV { get; set; }
    public double BatteryPrc { get; set; }
    public double RssiDbm { get; set; }
    public double RssiPrc { get; set; }
    public int? DistanceMm { get; set; }
    public double? WaterL { get; set; }
    public double? LevelFraction { get; set; }
    public double? RealLevelFraction { get; set; }
    public DateTime EstimatedNextRefresh { get; set; }
}

class AccountSensorResult
{
    public required AccountSensorDto AccountSensor { get; init; }
    public required LastMeasurementDto? LastMeasurement { get; init; }
}

[Route("api/a/{AccountLink}/s/{SensorLink}")]
public class AccountSensorController : Controller
{
    private readonly IMediator _mediator;
    
    public async Task<IActionResult> Index(string accountLink, string sensorLink)
    {
        var accountSensor = await _mediator.Send(new SensorByLinkQuery()
        {
            AccountLink = accountLink,
            SensorLink = sensorLink
        });

        if (accountSensor == null)
            return NotFound();
        
        var lastMeasurement = await _mediator.Send(new LastMeasurementQuery
            { DevEui = accountSensor.Sensor.DevEui });
        MeasurementEx? measurementEx;
        if (lastMeasurement != null)
            measurementEx = new MeasurementEx(lastMeasurement, accountSensor);
        else
            measurementEx = null;

        LastMeasurementDto? lastMeasurementDto;
        if (measurementEx != null)
        {
            lastMeasurementDto = new LastMeasurementDto
            {
                TimeStamp = measurementEx.Timestamp,
                BatV = measurementEx.BatV,
                BatteryPrc = measurementEx.BatteryPrc,
                RssiDbm = measurementEx.RssiDbm,
                RssiPrc = measurementEx.RssiPrc,
                DistanceMm = measurementEx.Distance.DistanceMm,
                WaterL = measurementEx.Distance.WaterL,
                LevelFraction = measurementEx.Distance.LevelFraction,
                RealLevelFraction = measurementEx.Distance.RealLevelFraction,
                EstimatedNextRefresh = measurementEx.EstimateNextRefresh()
            };
        }
        else
        {
            lastMeasurementDto = null;
        }

        var result = new AccountSensorResult
        {
            AccountSensor = new AccountSensorDto
            {
                Name = accountSensor.Name,
                CapacityL = accountSensor.CapacityL,
                ResolutionL = accountSensor.ResolutionL,
                DistanceMmEmpty = accountSensor.DistanceMmEmpty,
                DistanceMmFull = accountSensor.DistanceMmFull,
                CreateTimestamp = accountSensor.CreateTimestamp
            },
            LastMeasurement = lastMeasurementDto
        };
        
        return Ok(result);
    }

    public AccountSensorController(IMediator mediator)
    {
        _mediator = mediator;
    }
}