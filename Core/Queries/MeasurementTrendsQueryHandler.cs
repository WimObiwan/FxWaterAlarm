using Core.Entities;
using Core.Repositories;
using MediatR;

namespace Core.Queries;

public record MeasurementTrendsQuery : IRequest<Measurement?[]>
{
    public required string DevEui { get; init; }
    public required IEnumerable<DateTime> Timestamps { get; init; }

    public static MeasurementTrendsQuery FromTimeSpans(string devEui, IEnumerable<TimeSpan> timeSpans)
    {
        var now = DateTime.UtcNow;
        return new MeasurementTrendsQuery
        {
            DevEui = devEui,
            Timestamps = timeSpans.Select(t => now.Add(t)).ToArray()
        };
    }
}

public class MeasurementTrendsQueryHandler : IRequestHandler<MeasurementTrendsQuery, Measurement?[]>
{
    private readonly IMeasurementRepository _measurementRepository;

    public MeasurementTrendsQueryHandler(IMeasurementRepository measurementRepository)
    {
        _measurementRepository = measurementRepository;
    }

    public async Task<Measurement?[]> Handle(MeasurementTrendsQuery request, CancellationToken cancellationToken)
    {
        return await _measurementRepository.GetTrends(request.DevEui, request.Timestamps, cancellationToken);
    }
}