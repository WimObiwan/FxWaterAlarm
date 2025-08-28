using Core.Configuration;
using Core.Entities;
using Core.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Core.Commands;

public record RemoveMeasurementCommand : IRequest
{
    public required Guid SensorUid { get; init; }
    public required DateTime Timestamp { get; init; }
}

public class RemoveMeasurementCommandHandler : IRequestHandler<RemoveMeasurementCommand>
{
    private readonly IMeasurementLevelRepository _measurementLevelRepository;
    private readonly IMeasurementDetectRepository _measurementDetectRepository;
    private readonly IMeasurementMoistureRepository _measurementMoistureRepository;
    private readonly IMeasurementThermometerRepository _measurementThermometerRepository;
    private readonly WaterAlarmDbContext _dbContext;
    private readonly MeasurementRemovalOptions _options;

    public RemoveMeasurementCommandHandler(
        IMeasurementLevelRepository measurementLevelRepository,
        IMeasurementDetectRepository measurementDetectRepository,
        IMeasurementMoistureRepository measurementMoistureRepository,
        IMeasurementThermometerRepository measurementThermometerRepository,
        WaterAlarmDbContext dbContext,
        IOptions<MeasurementRemovalOptions> options)
    {
        _measurementLevelRepository = measurementLevelRepository;
        _measurementDetectRepository = measurementDetectRepository;
        _measurementMoistureRepository = measurementMoistureRepository;
        _measurementThermometerRepository = measurementThermometerRepository;
        _dbContext = dbContext;
        _options = options.Value;
    }

    public async Task Handle(RemoveMeasurementCommand request, CancellationToken cancellationToken)
    {
        // Find the sensor to determine its type and DevEUI
        var sensor = await _dbContext.Sensors
            .Where(s => s.Uid == request.SensorUid)
            .FirstOrDefaultAsync(cancellationToken);

        if (sensor == null)
            throw new InvalidOperationException($"Sensor with UID '{request.SensorUid}' not found");

        // Calculate time range based on tolerance
        var toleranceSpan = TimeSpan.FromSeconds(_options.TimestampToleranceSeconds);
        var fromTime = request.Timestamp.Subtract(toleranceSpan);
        var tillTime = request.Timestamp.Add(toleranceSpan);

        // Find and delete measurements based on sensor type
        switch (sensor.Type)
        {
            case SensorType.Level:
            case SensorType.LevelPressure:
                await ProcessLevelMeasurements(sensor.DevEui, fromTime, tillTime, cancellationToken);
                break;
            case SensorType.Detect:
                await ProcessDetectMeasurements(sensor.DevEui, fromTime, tillTime, cancellationToken);
                break;
            case SensorType.Moisture:
                await ProcessMoistureMeasurements(sensor.DevEui, fromTime, tillTime, cancellationToken);
                break;
            case SensorType.Thermometer:
                await ProcessThermometerMeasurements(sensor.DevEui, fromTime, tillTime, cancellationToken);
                break;
            default:
                throw new InvalidOperationException($"Unsupported sensor type: {sensor.Type}");
        }
    }

    private async Task ProcessLevelMeasurements(string devEui, DateTime fromTime, DateTime tillTime, CancellationToken cancellationToken)
    {
        var measurements = await _measurementLevelRepository.GetMeasurementsInTimeRange(devEui, fromTime, tillTime, cancellationToken);
        
        if (measurements.Length == 0)
            throw new InvalidOperationException("No measurements found within the specified time range");
        
        if (measurements.Length > 1)
            throw new InvalidOperationException($"Multiple measurements ({measurements.Length}) found within the specified time range. Expected exactly 1.");

        await _measurementLevelRepository.DeleteMeasurementsInTimeRange(devEui, fromTime, tillTime, cancellationToken);
    }

    private async Task ProcessDetectMeasurements(string devEui, DateTime fromTime, DateTime tillTime, CancellationToken cancellationToken)
    {
        var measurements = await _measurementDetectRepository.GetMeasurementsInTimeRange(devEui, fromTime, tillTime, cancellationToken);
        
        if (measurements.Length == 0)
            throw new InvalidOperationException("No measurements found within the specified time range");
        
        if (measurements.Length > 1)
            throw new InvalidOperationException($"Multiple measurements ({measurements.Length}) found within the specified time range. Expected exactly 1.");

        await _measurementDetectRepository.DeleteMeasurementsInTimeRange(devEui, fromTime, tillTime, cancellationToken);
    }

    private async Task ProcessMoistureMeasurements(string devEui, DateTime fromTime, DateTime tillTime, CancellationToken cancellationToken)
    {
        var measurements = await _measurementMoistureRepository.GetMeasurementsInTimeRange(devEui, fromTime, tillTime, cancellationToken);
        
        if (measurements.Length == 0)
            throw new InvalidOperationException("No measurements found within the specified time range");
        
        if (measurements.Length > 1)
            throw new InvalidOperationException($"Multiple measurements ({measurements.Length}) found within the specified time range. Expected exactly 1.");

        await _measurementMoistureRepository.DeleteMeasurementsInTimeRange(devEui, fromTime, tillTime, cancellationToken);
    }

    private async Task ProcessThermometerMeasurements(string devEui, DateTime fromTime, DateTime tillTime, CancellationToken cancellationToken)
    {
        var measurements = await _measurementThermometerRepository.GetMeasurementsInTimeRange(devEui, fromTime, tillTime, cancellationToken);
        
        if (measurements.Length == 0)
            throw new InvalidOperationException("No measurements found within the specified time range");
        
        if (measurements.Length > 1)
            throw new InvalidOperationException($"Multiple measurements ({measurements.Length}) found within the specified time range. Expected exactly 1.");

        await _measurementThermometerRepository.DeleteMeasurementsInTimeRange(devEui, fromTime, tillTime, cancellationToken);
    }
}