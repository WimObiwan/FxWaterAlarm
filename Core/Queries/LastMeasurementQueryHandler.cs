using Core.Entities;
using Core.Repositories;
using MediatR;

namespace Core.Queries;

public record LastMeasurementQuery : IRequest<MeasurementLevel?>
{
    public required string DevEui { get; init; }
}

public class LastMeasurementQueryHandler : IRequestHandler<LastMeasurementQuery, MeasurementLevel?>
{
    private readonly IMeasurementRepository _measurementRepository;

    public LastMeasurementQueryHandler(IMeasurementRepository measurementRepository)
    {
        _measurementRepository = measurementRepository;
    }

    public async Task<MeasurementLevel?> Handle(LastMeasurementQuery request, CancellationToken cancellationToken)
    {
        return await _measurementRepository.GetLast(request.DevEui, cancellationToken);
    }
}