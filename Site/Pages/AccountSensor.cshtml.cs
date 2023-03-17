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
    public double? RssiPrc { get; private set; }
    public double? BatteryPrc { get; private set; }

    public async Task OnGet(string accountLink, string sensorLink)
    {
        AccountSensorEntity = await _mediator.Send(new SensorByLinkQuery
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
                // -150 = 0%  --> -100 = 80%

                RssiPrc = (LastMeasurement.RssiDbm + 150.0) / 60.0 * 80.0;
                BatteryPrc = (LastMeasurement.BatV - 3.0) / 0.335 * 100.0;

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
                }
            }
        }
    }
}