using Core.Entities;
using Core.Repositories;

namespace CoreTests;

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
