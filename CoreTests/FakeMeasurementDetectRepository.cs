using Core.Entities;
using Core.Repositories;

namespace CoreTests;

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
