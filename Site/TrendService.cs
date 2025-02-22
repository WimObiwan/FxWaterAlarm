using Core.Entities;
using Core.Queries;
using Core.Util;
using MediatR;
using Site.Utilities;

namespace Site;

public interface ITrendService
{
    Task<TrendMeasurementEx?> GetTrendMeasurement(TimeSpan timeSpan, MeasurementLevelEx lastMeasurementLevelEx);
    Task<TrendMeasurementEx?[]> GetTrendMeasurements(MeasurementLevelEx lastMeasurementLevelEx, params TimeSpan[] fromHours);
}

public class TrendService : ITrendService
{
    private readonly IMediator _mediator;

    public TrendService(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    public async Task<TrendMeasurementEx?> GetTrendMeasurement(TimeSpan timeSpan, MeasurementLevelEx lastMeasurementLevelEx)
    {
        var trendMeasurement = await _mediator.Send(
            new MeasurementLastBeforeQuery<MeasurementLevel>
            {
                DevEui = lastMeasurementLevelEx.AccountSensor.Sensor.DevEui,
                Timestamp = lastMeasurementLevelEx.Timestamp.Add(-timeSpan)
            });
        if (trendMeasurement == null)
            return null;
        return new TrendMeasurementEx(timeSpan, trendMeasurement, lastMeasurementLevelEx);        
    }

    public async Task<TrendMeasurementEx?[]> GetTrendMeasurements(MeasurementLevelEx lastMeasurementLevelEx, params TimeSpan[] fromHours)
    {
        int len = fromHours.Length;
        TrendMeasurementEx?[] trendMeasurementExes = new TrendMeasurementEx[len];

        for (int i = 0; i < len; i++)
        {
            trendMeasurementExes[i] = await GetTrendMeasurement(fromHours[i], lastMeasurementLevelEx);
        }

        return trendMeasurementExes;
    }
}