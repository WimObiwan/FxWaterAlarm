using Core.Queries;
using Core.Util;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Site.Pages;
using Site.Utilities;

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
    public DateTime TimeStamp { get; init; }
    public double BatV { get; init; }
    public double BatteryPrc { get; init; }
    public double RssiDbm { get; init; }
    public double RssiPrc { get; init; }
    public int? DistanceMm { get; init; }
    public int? HeightMm { get; init; }
    public double? WaterL { get; init; }
    public double? LevelFraction { get; init; }
    public double? RealLevelFraction { get; init; }
    public DateTime EstimatedNextRefresh { get; init; }
}

class Trend
{
    public Trend(TrendMeasurementEx? trendMeasurementEx)
    {
        DifferenceHeightMm = trendMeasurementEx?.DifferenceHeight;
        DifferenceWaterL = trendMeasurementEx?.DifferenceWaterL;
        DifferenceLevelFraction = trendMeasurementEx?.DifferenceLevelFraction;
        DifferenceHeightMmPerDay = trendMeasurementEx?.DifferenceHeightPerDay;
        DifferenceWaterLPerDay = trendMeasurementEx?.DifferenceWaterLPerDay;
        DifferenceLevelFractionPerDay = trendMeasurementEx?.DifferenceLevelFractionPerDay;
    }
    
    public double? DifferenceHeightMm { get; init; }
    public double? DifferenceWaterL { get; init; }
    public double? DifferenceLevelFraction { get; init; }
    public double? DifferenceHeightMmPerDay { get; init; }
    public double? DifferenceWaterLPerDay { get; init; }
    public double? DifferenceLevelFractionPerDay { get; init; }
}

class TrendsDto
{
    //public required Trend Trend1H { get; init; }
    public required Trend Trend6H { get; init; }
    public required Trend Trend24H { get; init; }
    public required Trend Trend7D { get; init; }
    public required Trend Trend30D { get; init; }
}

class AccountSensorResult
{
    public required AccountSensorDto AccountSensor { get; init; }
    public required LastMeasurementDto? LastMeasurement { get; init; }
    public required TrendsDto? Trends { get; init; }
}

[Route("api/a/{AccountLink}/s/{SensorLink}")]
public class AccountSensorController : Controller
{
    private readonly IMediator _mediator;
    private readonly ITrendService _trendService;

    public async Task<IActionResult> Index(string accountLink, string sensorLink)
    {
        var accountSensor = await _mediator.Send(new AccountSensorByLinkQuery()
        {
            AccountLink = accountLink,
            SensorLink = sensorLink
        });

        if (accountSensor == null)
            return NotFound();
        
        var lastMeasurement = await _mediator.Send(new LastMeasurementLevelQuery
            { DevEui = accountSensor.Sensor.DevEui });
        var measurementMevelEx = lastMeasurement != null ? new MeasurementLevelEx(lastMeasurement, accountSensor) : null;

        LastMeasurementDto? lastMeasurementDto;
        TrendsDto? trendsDto;
        if (measurementMevelEx != null)
        {
            lastMeasurementDto = new LastMeasurementDto
            {
                TimeStamp = measurementMevelEx.Timestamp,
                BatV = measurementMevelEx.BatV,
                BatteryPrc = measurementMevelEx.BatteryPrc,
                RssiDbm = measurementMevelEx.RssiDbm,
                RssiPrc = measurementMevelEx.RssiPrc,
                DistanceMm = measurementMevelEx.Distance.DistanceMm,
                HeightMm = measurementMevelEx.Distance.HeightMm,
                WaterL = measurementMevelEx.Distance.WaterL,
                LevelFraction = measurementMevelEx.Distance.LevelFraction,
                RealLevelFraction = measurementMevelEx.Distance.RealLevelFraction,
                EstimatedNextRefresh = measurementMevelEx.EstimateNextRefresh()
            };
            
            var trendMeasurements = await _trendService.GetTrendMeasurements(measurementMevelEx,
                //TimeSpan.FromHours(1),
                TimeSpan.FromHours(6),
                TimeSpan.FromHours(24),
                TimeSpan.FromDays(7),
                TimeSpan.FromDays(30));

            trendsDto = new TrendsDto()
            {
                //Trend1H = new Trend(trendMeasurements[0]),
                Trend6H = new Trend(trendMeasurements[0]),
                Trend24H = new Trend(trendMeasurements[1]),
                Trend7D = new Trend(trendMeasurements[2]),
                Trend30D = new Trend(trendMeasurements[3])
            };
        }
        else
        {
            lastMeasurementDto = null;
            trendsDto = null;
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
            LastMeasurement = lastMeasurementDto,
            Trends = trendsDto
        };
        
        return Ok(result);
    }

    public AccountSensorController(IMediator mediator, ITrendService trendService)
    {
        _mediator = mediator;
        _trendService = trendService;
    }
}