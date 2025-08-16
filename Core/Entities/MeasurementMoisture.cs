using System.Collections.Immutable;

namespace Core.Entities;

public record MeasurementMoisture : Measurement
{
    public double SoilMoisturePrc { get; init; }
    public double SoilConductivity { get; init; }
    public double SoilTemperature { get; init; }

    public override IReadOnlyDictionary<string, object> GetValues()
    {
        var values = new Dictionary<string, object>
        {
            ["SoilMoisturePrc"] = SoilMoisturePrc,
            ["SoilConductivity"] = SoilConductivity,
            ["SoilTemperature"] = SoilTemperature
        };
        return values.ToImmutableDictionary();
    }
}