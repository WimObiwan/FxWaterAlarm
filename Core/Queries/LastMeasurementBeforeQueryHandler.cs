using Core.Entities;
using Core.Repositories;
using Core.Util;
using MediatR;

namespace Core.Queries;

public record LastMeasurementBeforeQuery : IRequest<IMeasurementEx?>
{
    public required AccountSensor AccountSensor { get; init; }
    public DateTime Timestamp { get; init; }
}

public class LastMeasurementBeforeQueryHandler : IRequestHandler<LastMeasurementBeforeQuery, IMeasurementEx?>
{
    private readonly IMeasurementLevelRepository _measurementLevelRepository;

    public LastMeasurementBeforeQueryHandler(
        IMeasurementLevelRepository measurementLevelRepository)
    {
        _measurementLevelRepository = measurementLevelRepository;
    }

    public async Task<IMeasurementEx?> Handle(LastMeasurementBeforeQuery request, CancellationToken cancellationToken)
    {
        var accountSensor = request.AccountSensor;
        switch (accountSensor.Sensor.Type)
        {
            case SensorType.Level:
            case SensorType.LevelPressure:
                return await GetLastMeasurementLevel(accountSensor, request.Timestamp, cancellationToken);
            default:
                return null;
        }
    }

    private async Task<IMeasurementEx?> GetLastMeasurementLevel(AccountSensor accountSensor, DateTime timestamp, CancellationToken cancellationToken)
    {
        var lastMeasurement = await _measurementLevelRepository.GetLastBefore(accountSensor.Sensor.DevEui, timestamp, cancellationToken);
        if (lastMeasurement != null)
            return new MeasurementLevelEx(lastMeasurement, accountSensor);
        else
            return null;
    }
}