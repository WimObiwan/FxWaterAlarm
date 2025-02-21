namespace Core.Entities;

public record MeasurementDetect : Measurement
{
    public int Status { get; init; }
}