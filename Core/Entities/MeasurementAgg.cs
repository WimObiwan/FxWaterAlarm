namespace Core.Entities;

public class MeasurementAgg
{
    public required string DevEui { get; init; }
    public DateTime Timestamp { get; init; }
    public int LastDistanceMm { get; init; }
    public int MinDistanceMm { get; init; }
    public int MeanDistanceMm { get; init; }
    public int MaxDistanceMm { get; init; }
    public double BatV { get; init; }
    public double RssiDbm { get; init; }
}