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
    private readonly IMeasurementLevelRepository _measurementLevelRepository;

    public LastMedianMeasurementQueryHandler(IMeasurementLevelRepository measurementLevelRepository)
    {
        _measurementLevelRepository = measurementLevelRepository;
    }

    public async Task<AggregatedMeasurement?> Handle(LastMedianMeasurementQuery request, CancellationToken cancellationToken)
    {
        return await _measurementLevelRepository.GetLastMedian(request.DevEui, request.From, cancellationToken);
    }
}