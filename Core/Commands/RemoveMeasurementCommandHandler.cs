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
                await ProcessMeasurements(
                    sensor.DevEui,
                    request.Timestamp,
                    fromTime,
                    tillTime,
                    cancellationToken,
                    _measurementLevelRepository.GetMeasurementsInTimeRange,
                    _measurementLevelRepository.DeleteMeasurementsInTimeRange);
                break;
            case SensorType.Detect:
                await ProcessMeasurements(
                    sensor.DevEui,
                    request.Timestamp,
                    fromTime,
                    tillTime,
                    cancellationToken,
                    _measurementDetectRepository.GetMeasurementsInTimeRange,
                    _measurementDetectRepository.DeleteMeasurementsInTimeRange);
                break;
            case SensorType.Moisture:
                await ProcessMeasurements(
                    sensor.DevEui,
                    request.Timestamp,
                    fromTime,
                    tillTime,
                    cancellationToken,
                    _measurementMoistureRepository.GetMeasurementsInTimeRange,
                    _measurementMoistureRepository.DeleteMeasurementsInTimeRange);
                break;
            case SensorType.Thermometer:
                await ProcessMeasurements(
                    sensor.DevEui,
                    request.Timestamp,
                    fromTime,
                    tillTime,
                    cancellationToken,
                    _measurementThermometerRepository.GetMeasurementsInTimeRange,
                    _measurementThermometerRepository.DeleteMeasurementsInTimeRange);
                break;
            default:
                throw new InvalidOperationException($"Unsupported sensor type: {sensor.Type}");
        }
    }

    private static async Task ProcessMeasurements<TMeasurement>(
        string devEui,
        DateTime requestedTimestamp,
        DateTime fromTime,
        DateTime tillTime,
        CancellationToken cancellationToken,
        Func<string, DateTime, DateTime, CancellationToken, Task<TMeasurement[]>> getMeasurementsInTimeRange,
        Func<string, DateTime, DateTime, CancellationToken, Task> deleteMeasurementsInTimeRange)
        where TMeasurement : Measurement
    {
        var measurements = await getMeasurementsInTimeRange(devEui, fromTime, tillTime, cancellationToken);

        if (measurements.Length == 0)
            throw new InvalidOperationException("No measurements found within the specified time range");

        var ordered = measurements
            .Select(m => new
            {
                Measurement = m,
                Distance = Math.Abs((m.Timestamp - requestedTimestamp).Ticks)
            })
            .OrderBy(x => x.Distance)
            .ThenBy(x => x.Measurement.Timestamp)
            .ToArray();

        if (ordered.Length > 1 && ordered[0].Distance == ordered[1].Distance)
            throw new InvalidOperationException(
                "Multiple measurements are equally close to the specified timestamp. Specify a more precise timestamp.");

        var measurementToDelete = ordered[0].Measurement;

        await deleteMeasurementsInTimeRange(
            devEui,
            measurementToDelete.Timestamp,
            measurementToDelete.Timestamp,
            cancellationToken);
    }
}