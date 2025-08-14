using Core.Util;

namespace Site.Utilities;

public static class MeasurementDisplayExtensions
{
    public static bool IsOld(this IMeasurementEx measurement, TimeSpan? threshold)
    {
        if (threshold == null)
            return false;
        
        return DateTime.UtcNow - measurement.Timestamp > threshold;
    }
}