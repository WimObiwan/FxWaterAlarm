namespace Core.Entities;

public record MeasurementMoisture : Measurement
{
    public double SoilMoisturePrc { get; init; }
    public double SoilConductivity { get; init; }
    public double SoilTemperature { get; init; }
}