using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;

namespace Core.Configuration;

public class MeasurementRemovalOptions
{
    public const string Location = "MeasurementRemoval";

    [ConfigurationKeyName("TimestampToleranceSeconds")]
    public required int TimestampToleranceSeconds { get; init; } = 5;
}