using Core.Entities;
using Core.Repositories;
using MediatR;

namespace Core.Queries;

public record AggregatedMeasurementsQuery : IRequest<AggregatedMeasurement[]>
{
    public required string DevEui { get; init; }
    public DateTime? From { get; init; }
    public DateTime? Till { get; init; }
    public required TimeSpan Interval { get; init; }
}

public class AggregatedMeasurementsQueryHandler : IRequestHandler<AggregatedMeasurementsQuery, AggregatedMeasurement[]>
{
    private readonly IMeasurementRepository _measurementRepository;

    public AggregatedMeasurementsQueryHandler(IMeasurementRepository measurementRepository)
    {
        _measurementRepository = measurementRepository;
    }

    public async Task<AggregatedMeasurement[]> Handle(AggregatedMeasurementsQuery request, CancellationToken cancellationToken)
    {
        return await _measurementRepository.GetAggregated(request.DevEui, request.From, request.Till, request.Interval,
            cancellationToken);
    }
}