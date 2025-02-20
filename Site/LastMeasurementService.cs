using Core.Entities;
using Core.Queries;
using Core.Util;
using MediatR;
using Site.Utilities;

namespace Site;

public interface ILastMeasurementService
{
    Task<MeasurementLevelEx?> GetLastMeasurement(AccountSensor accountSensor);
}

public class LastMeasurementService : ILastMeasurementService
{
    private readonly IMediator _mediator;

    public LastMeasurementService(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    public async Task<MeasurementLevelEx?> GetLastMeasurement(AccountSensor accountSensor)
    {
        switch (accountSensor.Sensor.Type)
        {
            // case SensorType.Detect:
            //     return await GetLastMeasurementDetect(accountSensor);
            case SensorType.Level:
                return await GetLastMeasurementLevel(accountSensor);
            default:
                return null;
        }
    }

    public async Task<MeasurementLevelEx?> GetLastMeasurementLevel(AccountSensor accountSensor)
    {
        var lastMeasurement = await _mediator.Send(new LastMeasurementLevelQuery
            { DevEui = accountSensor.Sensor.DevEui });
        if (lastMeasurement != null)
            return new MeasurementLevelEx(lastMeasurement, accountSensor);
        else
            return null;
    }
}