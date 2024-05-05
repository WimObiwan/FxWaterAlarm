using Core.Entities;
using Core.Repositories;
using MediatR;

namespace Core.Queries;

public record LastMedianMeasurementQuery : IRequest<AggregatedMeasurement?>
{
    public required string DevEui { get; init; }
    public required DateTime From { get; init; }
}

public class LastMedianMeasurementQueryHandler : IRequestHandler<LastMedianMeasurementQuery, AggregatedMeasurement?>
{
    private readonly IMeasurementRepository _measurementRepository;

    public LastMedianMeasurementQueryHandler(IMeasurementRepository measurementRepository)
    {
        _measurementRepository = measurementRepository;
    }

    public async Task<AggregatedMeasurement?> Handle(LastMedianMeasurementQuery request, CancellationToken cancellationToken)
    {
        return await _measurementRepository.GetLastMedian(request.DevEui, request.From, cancellationToken);
    }
}