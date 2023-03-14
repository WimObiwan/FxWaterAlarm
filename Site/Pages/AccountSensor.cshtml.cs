using Core.Entities;
using Core.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Site.Pages;

public class AccountSensor : PageModel
{
    private readonly IMediator _mediator;

    public AccountSensor(IMediator mediator)
    {
        _mediator = mediator;
    }

    public Measurement? LastMeasurement { get; private set; }

    public Core.Entities.AccountSensor? AccountSensorEntity { get; private set; }
    public double? LevelPrc { get; private set; }
    public double? WaterL { get; private set; }
    public double? ResolutionL { get; private set; }

    public async Task OnGet(string accountLink, string sensorLink)
    {
        AccountSensorEntity = await _mediator.Send(new ReadSensorByLinkQuery
        {
            SensorLink = sensorLink,
            AccountLink = accountLink
        });

        if (AccountSensorEntity != null)
        {
            LastMeasurement = await _mediator.Send(new LastMeasurementQuery
                { DevEui = AccountSensorEntity.Sensor.DevEui });
            if (LastMeasurement != null)
            {
                if (AccountSensorEntity.DistanceMmEmpty.HasValue && AccountSensorEntity.DistanceMmFull.HasValue)
                {
                    var levelFraction
                        = ((double)AccountSensorEntity.DistanceMmEmpty.Value - LastMeasurement.DistanceMm)
                          / (AccountSensorEntity.DistanceMmEmpty.Value - AccountSensorEntity.DistanceMmFull.Value);
                    LevelPrc = levelFraction * 100.0;
                    if (AccountSensorEntity.CapacityL.HasValue)
                    {
                        WaterL = levelFraction * AccountSensorEntity.CapacityL.Value;
                        ResolutionL =
                            1.0 / (AccountSensorEntity.DistanceMmEmpty.Value - AccountSensorEntity.DistanceMmFull.Value)
                            * AccountSensorEntity.CapacityL.Value;
                    }
                    else
                    {
                        WaterL = null;
                    }
                }
                else
                {
                    LevelPrc = null;
                    WaterL = null;
                }
            }
            else
            {
                LevelPrc = null;
                WaterL = null;
            }
        }
    }
}