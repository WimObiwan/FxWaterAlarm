using Core.Entities;
using Core.Queries;
using Core.Util;
using MediatR;
using Site.Utilities;

namespace Site;

public interface ILastMeasurementService
{
    Task<IMeasurementEx?> GetLastMeasurement(AccountSensor accountSensor);
}

public class LastMeasurementService : ILastMeasurementService
{
    private readonly IMediator _mediator;

    public LastMeasurementService(IMediator mediator)
    {
        _mediator = mediator;
    }
    
    public async Task<IMeasurementEx?> GetLastMeasurement(AccountSensor accountSensor)
    {
        switch (accountSensor.Sensor.Type)
        {
            case SensorType.Detect:
                return await GetLastMeasurementDetect(accountSensor);
            case SensorType.Level:
                return await GetLastMeasurementLevel(accountSensor);
            default:
                return null;
        }
    }

    private async Task<IMeasurementEx?> GetLastMeasurementLevel(AccountSensor accountSensor)
    {
        var lastMeasurement = await _mediator.Send(new LastMeasurementLevelQuery
            { DevEui = accountSensor.Sensor.DevEui });
        if (lastMeasurement != null)
            return new MeasurementLevelEx(lastMeasurement, accountSensor);
        else
            return null;
    }

    private async Task<IMeasurementEx?> GetLastMeasurementDetect(AccountSensor accountSensor)
    {
        var lastMeasurement = await _mediator.Send(new LastMeasurementDetectQuery
            { DevEui = accountSensor.Sensor.DevEui });
        if (lastMeasurement != null)
            return new MeasurementDetectEx(lastMeasurement, accountSensor);
        else
            return null;
    }
}