using System.Collections.Immutable;

namespace Core.Entities;

public record MeasurementLevel : Measurement
{
    public int DistanceMm { get; init; }

    public override IReadOnlyDictionary<string, object> GetValues()
    {
        var values = new Dictionary<string, object>
        {
            ["DistanceMm"] = DistanceMm
        };
        return values.ToImmutableDictionary();
    }
}