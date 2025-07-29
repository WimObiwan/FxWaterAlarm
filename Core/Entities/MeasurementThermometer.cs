namespace Core.Entities;

public record MeasurementThermometer : Measurement
{
    public double TempC { get; init; }
    public double HumPrc { get; init; }
}