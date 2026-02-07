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
