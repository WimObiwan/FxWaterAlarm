using Core.Entities;
using Core.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Site.Pages;

public class AccountSensor : PageModel
{
    private readonly IMediator _mediator;
    private readonly ILastMeasurementQuery _lastMeasurementQuery;

    public AccountSensor(IMediator mediator, ILastMeasurementQuery lastMeasurementQuery)
    {
        _mediator = mediator;
        _lastMeasurementQuery = lastMeasurementQuery;
    }

    public Measurement? LastMeasurement { get; private set; }

    public double? LevelPrc { get; private set; }

    public async Task OnGet(string accountLink, string sensorLink)
    {
        var sensor = await _mediator.Send(new ReadSensorByLinkQuery
        {
            SensorLink = sensorLink,
            AccountLink = accountLink
        });

        if (sensor == null)
        {
            //...
        }
        else
        {
            LastMeasurement = await _lastMeasurementQuery.Get(sensor.DevEui);
            if (LastMeasurement != null)
                LevelPrc = 100.0 * (2380.0 - LastMeasurement.DistanceMm) / (2380.0 - 380.0);
            else
                LevelPrc = null;
        }
    }
}