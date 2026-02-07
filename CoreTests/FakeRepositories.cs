using Core.Entities;
using Core.Repositories;

namespace CoreTests;

/// <summary>
/// Fake implementation of IMeasurementLevelRepository for testing.
/// </summary>
public class FakeMeasurementLevelRepository : IMeasurementLevelRepository
{
    public MeasurementLevel? LastResult { get; set; }
    public MeasurementLevel[]? GetResult { get; set; }
    public AggregatedMeasurement[]? AggregatedResult { get; set; }
    public MeasurementLevel? LastBeforeResult { get; set; }
    public AggregatedMeasurement? LastMedianResult { get; set; }

    public Task<MeasurementLevel?> GetLast(string devEui, CancellationToken cancellationToken) =>
        Task.FromResult(LastResult);

    public Task<MeasurementLevel[]> Get(string devEui, DateTime? from, DateTime? till, CancellationToken cancellationToken) =>
        Task.FromResult(GetResult ?? Array.Empty<MeasurementLevel>());

    public Task<AggregatedMeasurement[]> GetAggregated(string devEui, DateTime? from, DateTime? till, TimeSpan? interval, CancellationToken cancellationToken) =>
        Task.FromResult(AggregatedResult ?? Array.Empty<AggregatedMeasurement>());

    public Task<MeasurementLevel?> GetLastBefore(string devEui, DateTime dateTime, CancellationToken cancellationToken) =>
        Task.FromResult(LastBeforeResult);

    public Task<AggregatedMeasurement?> GetLastMedian(string devEui, DateTime from, CancellationToken cancellationToken) =>
        Task.FromResult(LastMedianResult);

    public Task Write(RecordLevel record, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task<MeasurementLevel[]> GetMeasurementsInTimeRange(string devEui, DateTime from, DateTime till, CancellationToken cancellationToken) =>
        Task.FromResult(GetResult ?? Array.Empty<MeasurementLevel>());

    public Task DeleteMeasurementsInTimeRange(string devEui, DateTime from, DateTime till, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}

/// <summary>
/// Fake implementation of IMeasurementDetectRepository for testing.
/// </summary>
public class FakeMeasurementDetectRepository : IMeasurementDetectRepository
{
    public MeasurementDetect? LastResult { get; set; }
    public MeasurementDetect[]? GetResult { get; set; }

    public Task<MeasurementDetect?> GetLast(string devEui, CancellationToken cancellationToken) =>
        Task.FromResult(LastResult);

    public Task<MeasurementDetect[]> Get(string devEui, DateTime? from, DateTime? till, CancellationToken cancellationToken) =>
        Task.FromResult(GetResult ?? Array.Empty<MeasurementDetect>());

    public Task<AggregatedMeasurement[]> GetAggregated(string devEui, DateTime? from, DateTime? till, TimeSpan? interval, CancellationToken cancellationToken) =>
        Task.FromResult(Array.Empty<AggregatedMeasurement>());

    public Task<MeasurementDetect?> GetLastBefore(string devEui, DateTime dateTime, CancellationToken cancellationToken) =>
        Task.FromResult<MeasurementDetect?>(null);

    public Task<AggregatedMeasurement?> GetLastMedian(string devEui, DateTime from, CancellationToken cancellationToken) =>
        Task.FromResult<AggregatedMeasurement?>(null);

    public Task Write(RecordDetect record, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task<MeasurementDetect[]> GetMeasurementsInTimeRange(string devEui, DateTime from, DateTime till, CancellationToken cancellationToken) =>
        Task.FromResult(GetResult ?? Array.Empty<MeasurementDetect>());

    public Task DeleteMeasurementsInTimeRange(string devEui, DateTime from, DateTime till, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}

/// <summary>
/// Fake implementation of IMeasurementMoistureRepository for testing.
/// </summary>
public class FakeMeasurementMoistureRepository : IMeasurementMoistureRepository
{
    public MeasurementMoisture? LastResult { get; set; }
    public MeasurementMoisture[]? GetResult { get; set; }

    public Task<MeasurementMoisture?> GetLast(string devEui, CancellationToken cancellationToken) =>
        Task.FromResult(LastResult);

    public Task<MeasurementMoisture[]> Get(string devEui, DateTime? from, DateTime? till, CancellationToken cancellationToken) =>
        Task.FromResult(GetResult ?? Array.Empty<MeasurementMoisture>());

    public Task<AggregatedMeasurement[]> GetAggregated(string devEui, DateTime? from, DateTime? till, TimeSpan? interval, CancellationToken cancellationToken) =>
        Task.FromResult(Array.Empty<AggregatedMeasurement>());

    public Task<MeasurementMoisture?> GetLastBefore(string devEui, DateTime dateTime, CancellationToken cancellationToken) =>
        Task.FromResult<MeasurementMoisture?>(null);

    public Task<AggregatedMeasurement?> GetLastMedian(string devEui, DateTime from, CancellationToken cancellationToken) =>
        Task.FromResult<AggregatedMeasurement?>(null);

    public Task Write(RecordMoisture record, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task<MeasurementMoisture[]> GetMeasurementsInTimeRange(string devEui, DateTime from, DateTime till, CancellationToken cancellationToken) =>
        Task.FromResult(GetResult ?? Array.Empty<MeasurementMoisture>());

    public Task DeleteMeasurementsInTimeRange(string devEui, DateTime from, DateTime till, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}

/// <summary>
/// Fake implementation of IMeasurementThermometerRepository for testing.
/// </summary>
public class FakeMeasurementThermometerRepository : IMeasurementThermometerRepository
{
    public MeasurementThermometer? LastResult { get; set; }
    public MeasurementThermometer[]? GetResult { get; set; }

    public Task<MeasurementThermometer?> GetLast(string devEui, CancellationToken cancellationToken) =>
        Task.FromResult(LastResult);

    public Task<MeasurementThermometer[]> Get(string devEui, DateTime? from, DateTime? till, CancellationToken cancellationToken) =>
        Task.FromResult(GetResult ?? Array.Empty<MeasurementThermometer>());

    public Task<AggregatedMeasurement[]> GetAggregated(string devEui, DateTime? from, DateTime? till, TimeSpan? interval, CancellationToken cancellationToken) =>
        Task.FromResult(Array.Empty<AggregatedMeasurement>());

    public Task<MeasurementThermometer?> GetLastBefore(string devEui, DateTime dateTime, CancellationToken cancellationToken) =>
        Task.FromResult<MeasurementThermometer?>(null);

    public Task<AggregatedMeasurement?> GetLastMedian(string devEui, DateTime from, CancellationToken cancellationToken) =>
        Task.FromResult<AggregatedMeasurement?>(null);

    public Task Write(RecordThermometer record, CancellationToken cancellationToken) =>
        Task.CompletedTask;

    public Task<MeasurementThermometer[]> GetMeasurementsInTimeRange(string devEui, DateTime from, DateTime till, CancellationToken cancellationToken) =>
        Task.FromResult(GetResult ?? Array.Empty<MeasurementThermometer>());

    public Task DeleteMeasurementsInTimeRange(string devEui, DateTime from, DateTime till, CancellationToken cancellationToken) =>
        Task.CompletedTask;
}
