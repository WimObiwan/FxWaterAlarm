using Core.Entities;

namespace Core.Util;

public class MeasurementEx
{
    private readonly Core.Entities.AccountSensor _accountSensor;
    private readonly Measurement _measurement;

    public MeasurementEx(Measurement measurement, Core.Entities.AccountSensor accountSensor)
    {
        _measurement = measurement;
        _accountSensor = accountSensor;
    }

    public DateTime EstimateNextRefresh()
    {
        // Refresh every 20 minutes
        const int refreshIntervalMinutes = 20;
        int nextRefreshMinutes = ((int)(DateTime.UtcNow - Timestamp).TotalMinutes / refreshIntervalMinutes + 1) * refreshIntervalMinutes;
        return Timestamp.AddSeconds(nextRefreshMinutes * 60 + 5);
    }

    public Core.Entities.AccountSensor AccountSensor => _accountSensor;
    public string DevEui => _accountSensor.Sensor.DevEui;
    public DateTime Timestamp => _measurement.Timestamp;
    public MeasurementDistance Distance => new(_measurement.DistanceMm, _accountSensor);
    public double BatV => _measurement.BatV;
    public double RssiDbm => _measurement.RssiDbm;
    public double RssiPrc => (_measurement.RssiDbm + 150.0) / 60.0 * 80.0;
    public double BatteryPrc => (_measurement.BatV - 3.0) / 0.335 * 100.0;
}
