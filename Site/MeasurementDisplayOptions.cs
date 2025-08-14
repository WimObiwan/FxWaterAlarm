using System.ComponentModel.DataAnnotations;

namespace Site;

public class MeasurementDisplayOptions
{
    public const string Location = "MeasurementDisplay";

    [ConfigurationKeyName("OldMeasurementThreshold")]
    public required TimeSpan? OldMeasurementThreshold { get; init; }
}