using Core.Util;

namespace Site.Utilities;

public static class MeasurementDisplayExtensions
{
    public static bool IsOld(this IMeasurementEx measurement, int? thresholdIntervals)
    {
        if (thresholdIntervals == null)
            return false;

        int interval = measurement.AccountSensor.Sensor.ExpectedIntervalSecs ?? 7200;
        var threshold = TimeSpan.FromSeconds((interval + 30) * thresholdIntervals.Value);
        return DateTime.UtcNow - measurement.Timestamp > threshold;
    }
}