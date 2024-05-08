using Core.Queries;
using Core.Util;
using MediatR;
using Site.Utilities;

namespace Site;

public interface ITrendService
{
    Task<TrendMeasurementEx?> GetTrendMeasurement(TimeSpan timeSpan, MeasurementEx lastMeasurement);
    Task<TrendMeasurementEx?[]> GetTrendMeasurements(MeasurementEx lastMeasurement, params TimeSpan[] fromHours);
}

public class TrendService : ITrendService
{
    private readonly IMediator _mediator;

    public TrendService(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    public async Task<TrendMeasurementEx?> GetTrendMeasurement(TimeSpan timeSpan, MeasurementEx lastMeasurement)
    {
        var trendMeasurement = await _mediator.Send(
            new MeasurementLastBeforeQuery
            {
                DevEui = lastMeasurement.AccountSensor.Sensor.DevEui,
                Timestamp = lastMeasurement.Timestamp.Add(-timeSpan)
            });
        if (trendMeasurement == null)
            return null;
        return new TrendMeasurementEx(timeSpan, trendMeasurement, lastMeasurement);        
    }

    public async Task<TrendMeasurementEx?[]> GetTrendMeasurements(MeasurementEx lastMeasurement, params TimeSpan[] fromHours)
    {
        int len = fromHours.Length;
        TrendMeasurementEx?[] trendMeasurementExes = new TrendMeasurementEx[len];

        for (int i = 0; i < len; i++)
        {
            trendMeasurementExes[i] = await GetTrendMeasurement(fromHours[i], lastMeasurement);
        }

        return trendMeasurementExes;
    }
}