namespace Core.Entities;

public class AggregatedMeasurement
{
    public required string DevEui { get; init; }
    public DateTime Timestamp { get; init; }
    public double BatV { get; init; }
    public double RssiDbm { get; init; }
}