using Core.Entities;
using Core.Repositories;
using MediatR;

namespace Core.Queries;

public record LastMeasurementDetectQuery : IRequest<MeasurementDetect?>
{
    public required string DevEui { get; init; }
}

public class LastMeasurementDetectQueryHandler : IRequestHandler<LastMeasurementDetectQuery, MeasurementDetect?>
{
    private readonly IMeasurementDetectRepository _measurementDetectRepository;

    public LastMeasurementDetectQueryHandler(IMeasurementDetectRepository measurementDetectRepository)
    {
        _measurementDetectRepository = measurementDetectRepository;
    }

    public async Task<MeasurementDetect?> Handle(LastMeasurementDetectQuery request, CancellationToken cancellationToken)
    {
        return await _measurementDetectRepository.GetLast(request.DevEui, cancellationToken);
    }
}