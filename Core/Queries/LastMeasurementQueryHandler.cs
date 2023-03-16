using Core.Entities;
using Core.Repositories;
using MediatR;

namespace Core.Queries;

public record LastMeasurementQuery : IRequest<Measurement?>
{
    public required string DevEui { get; init; }
}

public class LastMeasurementQueryHandler : IRequestHandler<LastMeasurementQuery, Measurement?>
{
    private readonly IMeasurementRepository _measurementRepository;

    public LastMeasurementQueryHandler(IMeasurementRepository measurementRepository)
    {
        _measurementRepository = measurementRepository;
    }

    public async Task<Measurement?> Handle(LastMeasurementQuery request, CancellationToken cancellationToken)
    {
        return await _measurementRepository.GetLast(request.DevEui, cancellationToken);
    }
}