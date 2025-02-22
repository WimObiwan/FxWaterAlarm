using Core.Entities;
using Core.Repositories;
using MediatR;

namespace Core.Queries;

public record MeasurementsQuery : IRequest<MeasurementLevel[]>
{
    public required string DevEui { get; init; }
    public DateTime? From { get; init; }
    public DateTime? Till { get; init; }
}

public class MeasurementsQueryHandler : IRequestHandler<MeasurementsQuery, MeasurementLevel[]>
{
    private readonly IMeasurementLevelRepository _measurementLevelRepository;

    public MeasurementsQueryHandler(IMeasurementLevelRepository measurementLevelRepository)
    {
        _measurementLevelRepository = measurementLevelRepository;
    }

    public async Task<MeasurementLevel[]> Handle(MeasurementsQuery request, CancellationToken cancellationToken)
    {
        return await _measurementLevelRepository.Get(request.DevEui, request.From, request.Till,
            cancellationToken);
    }
}