using Core.Entities;
using Core.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Core.Commands;

public record AddMeasurementCommand : IRequest
{
    public required string DevEui { get; init; }
    public required DateTime Timestamp { get; init; }
    public required Dictionary<string, object> Measurements { get; init; }
}

public class AddMeasurementCommandHandler : IRequestHandler<AddMeasurementCommand>
{
    private readonly IMeasurementLevelRepository _measurementLevelRepository;
    private readonly IMeasurementDetectRepository _measurementDetectRepository;
    private readonly IMeasurementMoistureRepository _measurementMoistureRepository;
    private readonly IMeasurementThermometerRepository _measurementThermometerRepository;
    private readonly WaterAlarmDbContext _dbContext;

    public AddMeasurementCommandHandler(
        IMeasurementLevelRepository measurementLevelRepository,
        IMeasurementDetectRepository measurementDetectRepository,
        IMeasurementMoistureRepository measurementMoistureRepository,
        IMeasurementThermometerRepository measurementThermometerRepository,
        WaterAlarmDbContext dbContext)
    {
        _measurementLevelRepository = measurementLevelRepository;
        _measurementDetectRepository = measurementDetectRepository;
        _measurementMoistureRepository = measurementMoistureRepository;
        _measurementThermometerRepository = measurementThermometerRepository;
        _dbContext = dbContext;
    }

    public async Task Handle(AddMeasurementCommand request, CancellationToken cancellationToken)
    {
        // Find the sensor to determine its type
        var sensor = await _dbContext.Sensors
            .Where(s => s.DevEui == request.DevEui)
            .FirstOrDefaultAsync(cancellationToken);

        if (sensor == null)
            throw new InvalidOperationException($"Sensor with DevEUI '{request.DevEui}' not found");

        // Extract common measurements
        var batV = GetMeasurementValue<double>(request.Measurements, "batV", "BatV");
        var rssi = GetMeasurementValue<int>(request.Measurements, "RSSI", "rssi", "RssiDbm");

        // Write measurement based on sensor type
        switch (sensor.Type)
        {
            case SensorType.Level:
            case SensorType.LevelPressure:
                await WriteLevel(request, batV, rssi, cancellationToken);
                break;
            case SensorType.Detect:
                await WriteDetect(request, batV, rssi, cancellationToken);
                break;
            case SensorType.Moisture:
                await WriteMoisture(request, batV, rssi, cancellationToken);
                break;
            case SensorType.Thermometer:
                await WriteThermometer(request, batV, rssi, cancellationToken);
                break;
            default:
                throw new NotSupportedException($"Sensor type '{sensor.Type}' is not supported");
        }
    }

    private async Task WriteLevel(AddMeasurementCommand request, double batV, int rssi, CancellationToken cancellationToken)
    {
        var distance = GetMeasurementValue<int>(request.Measurements, "distance", "Distance", "DistanceMm");
        
        var record = new RecordLevel
        {
            DevEui = request.DevEui,
            Timestamp = request.Timestamp,
            BatV = batV,
            Distance = distance,
            Rssi = rssi
        };

        await _measurementLevelRepository.Write(record, cancellationToken);
    }

    private async Task WriteDetect(AddMeasurementCommand request, double batV, int rssi, CancellationToken cancellationToken)
    {
        var status = GetMeasurementValue<int>(request.Measurements, "waterStatus", "Status");
        
        var record = new RecordDetect
        {
            DevEui = request.DevEui,
            Timestamp = request.Timestamp,
            BatV = batV,
            Status = status,
            Rssi = rssi
        };

        await _measurementDetectRepository.Write(record, cancellationToken);
    }

    private async Task WriteMoisture(AddMeasurementCommand request, double batV, int rssi, CancellationToken cancellationToken)
    {
        var soilMoisturePrc = GetMeasurementValue<double>(request.Measurements, "soilMoisturePrc", "SoilMoisturePrc");
        var soilConductivity = GetMeasurementValue<int>(request.Measurements, "soilConductivity", "SoilConductivity");
        var soilTemperature = GetMeasurementValue<double>(request.Measurements, "soilTemperature", "SoilTemperature");
        
        var record = new RecordMoisture
        {
            DevEui = request.DevEui,
            Timestamp = request.Timestamp,
            BatV = batV,
            SoilMoisturePrc = soilMoisturePrc,
            SoilConductivity = soilConductivity,
            SoilTemperature = soilTemperature,
            Rssi = rssi
        };

        await _measurementMoistureRepository.Write(record, cancellationToken);
    }

    private async Task WriteThermometer(AddMeasurementCommand request, double batV, int rssi, CancellationToken cancellationToken)
    {
        var tempC = GetMeasurementValue<double>(request.Measurements, "tempC", "TempC");
        var humPrc = GetMeasurementValue<double>(request.Measurements, "humPrc", "HumPrc");
        
        var record = new RecordThermometer
        {
            DevEui = request.DevEui,
            Timestamp = request.Timestamp,
            BatV = batV,
            TempC = tempC,
            HumPrc = humPrc,
            Rssi = rssi
        };

        await _measurementThermometerRepository.Write(record, cancellationToken);
    }

    private static T GetMeasurementValue<T>(Dictionary<string, object> measurements, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (measurements.TryGetValue(key, out var value))
            {
                if (value is T directValue)
                    return directValue;
                
                try
                {
                    // Handle JSON number conversion
                    if (typeof(T) == typeof(double) && value is System.Text.Json.JsonElement jsonElement)
                    {
                        if (jsonElement.ValueKind == System.Text.Json.JsonValueKind.Number)
                        {
                            return (T)(object)jsonElement.GetDouble();
                        }
                    }
                    else if (typeof(T) == typeof(int) && value is System.Text.Json.JsonElement jsonElementInt)
                    {
                        if (jsonElementInt.ValueKind == System.Text.Json.JsonValueKind.Number)
                        {
                            return (T)(object)jsonElementInt.GetInt32();
                        }
                    }
                    
                    return (T)Convert.ChangeType(value, typeof(T));
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Cannot convert measurement '{key}' value '{value}' to type {typeof(T).Name}", ex);
                }
            }
        }
        
        throw new ArgumentException($"Required measurement not found. Expected one of: {string.Join(", ", keys)}");
    }
}