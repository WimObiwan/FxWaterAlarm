using Core.Entities;
using Core.Repositories;
using MediatR;

namespace Core.Queries;

public record MeasurementLastBeforeQuery<TMeasurement> : IRequest<TMeasurement?> where TMeasurement : Measurement
{
    public required string DevEui { get; init; }
    public required DateTime Timestamp { get; init; }
}

public class MeasurementLastBeforeQueryHandler<TMeasurement> : IRequestHandler<MeasurementLastBeforeQuery<TMeasurement>, TMeasurement?> where TMeasurement : Measurement
{
    private readonly IMeasurementRepository _measurementRepository;

    public MeasurementLastBeforeQueryHandler(IMeasurementRepository measurementRepository)
    {
        _measurementRepository = measurementRepository;
    }

    public async Task<TMeasurement?> Handle(MeasurementLastBeforeQuery<TMeasurement> request, CancellationToken cancellationToken)
    {
        return (TMeasurement?)(Measurement?)await _measurementRepository.GetLastBefore(request.DevEui, request.Timestamp, cancellationToken);
    }
}