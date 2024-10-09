using Core.Entities;
using Core.Repositories;
using MediatR;

namespace Core.Queries;

public record LastMeasurementLevelQuery : IRequest<MeasurementLevel?>
{
    public required string DevEui { get; init; }
}

public class LastMeasurementLevelQueryHandler : IRequestHandler<LastMeasurementLevelQuery, MeasurementLevel?>
{
    private readonly IMeasurementLevelRepository _measurementLevelRepository;

    public LastMeasurementLevelQueryHandler(IMeasurementLevelRepository measurementLevelRepository)
    {
        _measurementLevelRepository = measurementLevelRepository;
    }

    public async Task<MeasurementLevel?> Handle(LastMeasurementLevelQuery request, CancellationToken cancellationToken)
    {
        return await _measurementLevelRepository.GetLast(request.DevEui, cancellationToken);
    }
}