using Core.Entities;

namespace Core.Util;

public class MeasurementEx<T> : IMeasurementEx where T : Measurement
{
    private readonly T _measurement;
    private readonly AccountSensor _accountSensor;

    public MeasurementEx(T measurement, AccountSensor accountSensor)
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

    public AccountSensor AccountSensor => _accountSensor;
    protected T Measurement => _measurement;

    public string DevEui => _accountSensor.Sensor.DevEui;
    public DateTime Timestamp => _measurement.Timestamp;
    public double BatV => _measurement.BatV;
    public double RssiDbm => _measurement.RssiDbm;
    public double RssiPrc => (_measurement.RssiDbm + 150.0) / 60.0 * 80.0;
    public double BatteryPrc => (_measurement.BatV - 3.0) / 0.335 * 100.0;
}