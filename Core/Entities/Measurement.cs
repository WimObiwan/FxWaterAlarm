using System.Collections.Immutable;

namespace Core.Entities;

public record Measurement
{
    public required string DevEui { get; init; }
    public DateTime Timestamp { get; init; }
    public double BatV { get; init; }
    public double RssiDbm { get; init; }

    public virtual IReadOnlyDictionary<string, object> GetValues()
    {
        return new Dictionary<string, object>().ToImmutableDictionary();
    }
}
