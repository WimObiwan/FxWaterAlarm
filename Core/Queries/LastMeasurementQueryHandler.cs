using Core.Entities;
using Core.Repositories;
using Core.Util;
using MediatR;

namespace Core.Queries;

public record LastMeasurementQuery : IRequest<IMeasurementEx?>
{
    public required AccountSensor AccountSensor { get; init; }
}

public class LastMeasurementQueryHandler : IRequestHandler<LastMeasurementQuery, IMeasurementEx?>
{
    private readonly IMeasurementLevelRepository _measurementLevelRepository;
    private readonly IMeasurementDetectRepository _measurementDetectRepository;

    public LastMeasurementQueryHandler(IMeasurementLevelRepository measurementLevelRepository, IMeasurementDetectRepository measurementDetectRepository)
    {
        _measurementLevelRepository = measurementLevelRepository;
        _measurementDetectRepository = measurementDetectRepository;
    }

    public async Task<IMeasurementEx?> Handle(LastMeasurementQuery request, CancellationToken cancellationToken)
    {
        var accountSensor = request.AccountSensor;
        switch (accountSensor.Sensor.Type)
        {
            case SensorType.Level:
                return await GetLastMeasurementLevel(accountSensor, cancellationToken);
            case SensorType.Detect:
                return await GetLastMeasurementDetect(accountSensor, cancellationToken);
            default:
                return null;
        }
    }

    private async Task<IMeasurementEx?> GetLastMeasurementLevel(AccountSensor accountSensor, CancellationToken cancellationToken)
    {
        var lastMeasurement = await _measurementLevelRepository.GetLast(accountSensor.Sensor.DevEui, cancellationToken);
        if (lastMeasurement != null)
            return new MeasurementLevelEx(lastMeasurement, accountSensor);
        else
            return null;
    }

    private async Task<IMeasurementEx?> GetLastMeasurementDetect(AccountSensor accountSensor, CancellationToken cancellationToken)
    {
        var lastMeasurement = await _measurementDetectRepository.GetLast(accountSensor.Sensor.DevEui, cancellationToken);
        if (lastMeasurement != null)
            return new MeasurementDetectEx(lastMeasurement, accountSensor);
        else
            return null;
    }
}