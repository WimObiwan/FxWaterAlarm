using Core.Entities;
using Core.Repositories;
using MediatR;

namespace Core.Queries;

public record MeasurementLastBeforeQuery : IRequest<Measurement?>
{
    public required string DevEui { get; init; }
    public required DateTime Timestamp { get; init; }
}

public class MeasurementLastBeforeQueryHandler : IRequestHandler<MeasurementLastBeforeQuery, Measurement?>
{
    private readonly IMeasurementRepository _measurementRepository;

    public MeasurementLastBeforeQueryHandler(IMeasurementRepository measurementRepository)
    {
        _measurementRepository = measurementRepository;
    }

    public async Task<Measurement?> Handle(MeasurementLastBeforeQuery request, CancellationToken cancellationToken)
    {
        return await _measurementRepository.GetLastBefore(request.DevEui, request.Timestamp, cancellationToken);
    }
}