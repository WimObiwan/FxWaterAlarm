using Core.Entities;
using Core.Queries;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Site.Pages;

public class Sensor : PageModel
{
    private readonly ILastMeasurementQuery _lastMeasurementQuery;

    public Sensor(ILastMeasurementQuery lastMeasurementQuery)
    {
        _lastMeasurementQuery = lastMeasurementQuery;
    }

    public Measurement? LastMeasurement { get; private set; }

    public double? LevelPrc { get; private set; }

    public async Task OnGet(string sensorId)
    {
        LastMeasurement = await _lastMeasurementQuery.Get(sensorId);
        if (LastMeasurement != null)
            LevelPrc = 100.0 * (2380.0 - LastMeasurement.DistanceMm) / (2380.0 - 380.0);
        else
            LevelPrc = null;
    }
}