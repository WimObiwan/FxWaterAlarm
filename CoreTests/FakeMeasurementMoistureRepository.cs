using Core.Entities;
using Core.Repositories;

namespace CoreTests;

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
