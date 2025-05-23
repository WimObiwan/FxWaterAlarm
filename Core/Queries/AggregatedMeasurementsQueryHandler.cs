// using Core.Entities;
// using Core.Repositories;
// using MediatR;

// namespace Core.Queries;

// public record AggregatedMeasurementsQuery : IRequest<AggregatedMeasurement[]>
// {
//     public required string DevEui { get; init; }
//     public DateTime? From { get; init; }
//     public DateTime? Till { get; init; }
//     public required TimeSpan Interval { get; init; }
// }

// public class AggregatedMeasurementsQueryHandler : IRequestHandler<AggregatedMeasurementsQuery, AggregatedMeasurement[]>
// {
//     private readonly IMeasurementLevelRepository _measurementLevelRepository;

//     public AggregatedMeasurementsQueryHandler(IMeasurementLevelRepository measurementLevelRepository)
//     {
//         _measurementLevelRepository = measurementLevelRepository;
//     }

//     public async Task<AggregatedMeasurement[]> Handle(AggregatedMeasurementsQuery request, CancellationToken cancellationToken)
//     {
//         if (request.Interval != TimeSpan.Zero)
//         {
//             return await _measurementLevelRepository.GetAggregated(request.DevEui, request.From, request.Till,
//                 request.Interval,
//                 cancellationToken);
//         }
//         else
//         {
//             var result = await _measurementLevelRepository.Get(request.DevEui, request.From, request.Till,
//                 cancellationToken);
//             return result.Select(i => new AggregatedMeasurementLevel()
//             {
//                 BatV = i.BatV,
//                 DevEui = i.DevEui,
//                 LastDistanceMm = i.DistanceMm,
//                 MaxDistanceMm = i.DistanceMm,
//                 MinDistanceMm = i.DistanceMm,
//                 MeanDistanceMm = i.DistanceMm,
//                 RssiDbm = i.RssiDbm,
//                 Timestamp = i.Timestamp
//             }).ToArray();
//         }
//     }
// }