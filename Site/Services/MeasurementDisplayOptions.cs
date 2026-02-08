using System.ComponentModel.DataAnnotations;

namespace Site.Services;

public class MeasurementDisplayOptions
{
    public const string Location = "MeasurementDisplay";

    [ConfigurationKeyName("OldMeasurementThresholdIntervals")]
    public required int? OldMeasurementThresholdIntervals { get; init; }
}