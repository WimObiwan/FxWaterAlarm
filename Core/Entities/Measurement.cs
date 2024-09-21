namespace Core.Entities;

public record Measurement
{
    public required string DevEui { get; init; }
    public DateTime Timestamp { get; init; }
    public int DistanceMm { get; init; }
    public double BatV { get; init; }
    public double RssiDbm { get; init; }
}