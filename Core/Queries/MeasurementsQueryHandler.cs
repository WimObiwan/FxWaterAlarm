using Core.Entities;
using Core.Repositories;
using Core.Util;
using MediatR;

namespace Core.Queries;

public record MeasurementsQuery : IRequest<IMeasurementEx[]?>
{
    public required AccountSensor AccountSensor { get; init; }
    public DateTime? From { get; init; }
    public DateTime? Till { get; init; }
}

public class MeasurementsQueryHandler : IRequestHandler<MeasurementsQuery, IMeasurementEx[]?>
{
    private readonly IMeasurementLevelRepository _measurementLevelRepository;
    private readonly IMeasurementDetectRepository _measurementDetectRepository;
    private readonly IMeasurementMoistureRepository _measurementMoistureRepository;

    public MeasurementsQueryHandler(
        IMeasurementLevelRepository measurementLevelRepository, 
        IMeasurementDetectRepository measurementDetectRepository,
        IMeasurementMoistureRepository measurementMoistureRepository)
    {
        _measurementLevelRepository = measurementLevelRepository;
        _measurementDetectRepository = measurementDetectRepository;
        _measurementMoistureRepository = measurementMoistureRepository;
    }

    public async Task<IMeasurementEx[]?> Handle(MeasurementsQuery request, CancellationToken cancellationToken)
    {
        var accountSensor = request.AccountSensor;
        switch (accountSensor.Sensor.Type)
        {
            case SensorType.Level:
                return await GetMeasurementsLevel(accountSensor, request.From, request.Till, cancellationToken);
            case SensorType.Detect:
                return await GetMeasurementsDetect(accountSensor, request.From, request.Till, cancellationToken);
            case SensorType.Moisture:
                return await GetMeasurementsMoisture(accountSensor, request.From, request.Till, cancellationToken);
            default:
                return null;
        }
    }

    private async Task<IMeasurementEx[]?> GetMeasurementsLevel(AccountSensor accountSensor, DateTime? from, DateTime? till, CancellationToken cancellationToken)
    {
        var measurements = await _measurementLevelRepository.Get(accountSensor.Sensor.DevEui, from, till, cancellationToken);
        if (measurements != null)
            return measurements.Select(m => new MeasurementLevelEx(m, accountSensor)).ToArray();
        else
            return null;
    }

    private async Task<IMeasurementEx[]?> GetMeasurementsDetect(AccountSensor accountSensor, DateTime? from, DateTime? till, CancellationToken cancellationToken)
    {
        var measurements = await _measurementDetectRepository.Get(accountSensor.Sensor.DevEui, from, till, cancellationToken);
        if (measurements != null)
            return measurements.Select(m => new MeasurementDetectEx(m, accountSensor)).ToArray();
        else
            return null;
    }

    private async Task<IMeasurementEx[]?> GetMeasurementsMoisture(AccountSensor accountSensor, DateTime? from, DateTime? till, CancellationToken cancellationToken)
    {
        var measurements = await _measurementMoistureRepository.Get(accountSensor.Sensor.DevEui, from, till, cancellationToken);
        if (measurements != null)
            return measurements.Select(m => new MeasurementMoistureEx(m, accountSensor)).ToArray();
        else
            return null;
    }
}