using System.Collections.Immutable;

namespace Core.Entities;

public record MeasurementDetect : Measurement
{
    public int Status { get; init; }

    public override IReadOnlyDictionary<string, object> GetValues()
    {
        var values = new Dictionary<string, object>
        {
            ["Status"] = Status
        };
        return values.ToImmutableDictionary();
    }
}