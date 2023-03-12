using Core.Entities;
using Core.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Site.Pages;

public class Sensor : PageModel
{
    private readonly ILastMeasurementQuery _lastMeasurementQuery;
    private readonly IMediator _mediator;

    public Sensor(IMediator mediator, ILastMeasurementQuery lastMeasurementQuery)
    {
        _mediator = mediator;
        _lastMeasurementQuery = lastMeasurementQuery;
    }

    public Measurement? LastMeasurement { get; private set; }

    public double? LevelPrc { get; private set; }

    public async Task OnGet(string sensorLink)
    {
        var sensor = await _mediator.Send(new ReadSensorByLinkQuery
        {
            Link = sensorLink
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