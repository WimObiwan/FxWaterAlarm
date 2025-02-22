namespace Core.Entities;

public class AggregatedMeasurementLevel : AggregatedMeasurement
{
    public int? LastDistanceMm { get; init; }
    public int? MinDistanceMm { get; init; }
    public int? MeanDistanceMm { get; init; }
    public int? MaxDistanceMm { get; init; }
}