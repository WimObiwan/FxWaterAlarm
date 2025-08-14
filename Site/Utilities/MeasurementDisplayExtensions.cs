using Core.Util;

namespace Site.Utilities;

public static class MeasurementDisplayExtensions
{
    public static bool IsOld(this IMeasurementEx measurement, TimeSpan threshold)
    {
        return DateTime.UtcNow - measurement.Timestamp > threshold;
    }
}