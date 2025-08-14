using System.ComponentModel.DataAnnotations;

namespace Site;

public class MeasurementDisplayOptions
{
    public const string Location = "MeasurementDisplay";

    [ConfigurationKeyName("OldMeasurementThreshold")]
    public required TimeSpan? OldMeasurementThresholdRaw { get; init; }

    public TimeSpan OldMeasurementThreshold =>
        OldMeasurementThresholdRaw
        ?? throw new Exception("MeasurementDisplayOptions.OldMeasurementThreshold not configured");
}