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
        // Use the sensor's expected interval instead of hardcoded value
        int refreshIntervalSecs = _accountSensor.Sensor.ExpectedIntervalSecs ?? 7200;
        int nextRefreshSecs = ((int)(DateTime.UtcNow - Timestamp).TotalSeconds / refreshIntervalSecs + 1) * refreshIntervalSecs;
        return Timestamp.AddSeconds(nextRefreshSecs + 5);
    }

    public AccountSensor AccountSensor => _accountSensor;
    protected T Measurement => _measurement;
    public IReadOnlyDictionary<string, object> GetValues() => _measurement.GetValues();

    public string DevEui => _accountSensor.Sensor.DevEui;
    public DateTime Timestamp => _measurement.Timestamp;
    public double BatV => _measurement.BatV;
    public double RssiDbm => _measurement.RssiDbm;
    public double RssiPrc => (_measurement.RssiDbm + 150.0) / 60.0 * 80.0;
    public double BatteryPrc => (_measurement.BatV - 3.0) / 0.335 * 100.0;
}