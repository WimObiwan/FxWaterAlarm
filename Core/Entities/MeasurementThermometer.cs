using System.Collections.Immutable;

namespace Core.Entities;

public record MeasurementThermometer : Measurement
{
    public double TempC { get; init; }
    public double HumPrc { get; init; }

    public override IReadOnlyDictionary<string, object> GetValues()
    {
        var values = new Dictionary<string, object>
        {
            ["TempC"] = TempC,
            ["HumPrc"] = HumPrc
        };
        return values.ToImmutableDictionary();
    }
}