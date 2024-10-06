namespace Core.Entities;

public record MeasurementLevel : Measurement
{
    public int DistanceMm { get; init; }
}