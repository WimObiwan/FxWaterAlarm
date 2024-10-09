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
    private readonly IMeasurementLevelRepository _measurementLevelRepository;

    public LastMeasurementDetectQueryHandler(IMeasurementLevelRepository measurementLevelRepository)
    {
        _measurementLevelRepository = measurementLevelRepository;
    }

    public /*async*/ Task<MeasurementDetect?> Handle(LastMeasurementDetectQuery request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
        // return await _measurementDetectRepository.GetLast(request.DevEui, cancellationToken);
    }
}