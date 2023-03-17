using Core.Entities;
using Core.Repositories;
using MediatR;

namespace Core.Queries;

public record MeasurementsQuery : IRequest<Measurement[]>
{
    public required string DevEui { get; init; }
    public required DateTime From { get; init; }
    public DateTime? Till { get; init; }
}

public class MeasurementsQueryHandler : IRequestHandler<MeasurementsQuery, Measurement[]>
{
    private readonly IMeasurementRepository _measurementRepository;

    public MeasurementsQueryHandler(IMeasurementRepository measurementRepository)
    {
        _measurementRepository = measurementRepository;
    }

    public async Task<Measurement[]> Handle(MeasurementsQuery request, CancellationToken cancellationToken)
    {
        return await _measurementRepository.Get(request.DevEui, request.From, request.Till, cancellationToken);
    }
}