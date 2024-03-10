using Core.Entities;

namespace Site.Utilities;

public class MeasurementAggEx
{
    private readonly Core.Entities.AccountSensor _accountSensor;
    private readonly AggregatedMeasurement _aggregatedMeasurement;

    public MeasurementAggEx(AggregatedMeasurement aggregatedMeasurement, Core.Entities.AccountSensor accountSensor)
    {
        _aggregatedMeasurement = aggregatedMeasurement;
        _accountSensor = accountSensor;
    }

    public string DevEui => _aggregatedMeasurement.DevEui;
    public DateTime Timestamp => _aggregatedMeasurement.Timestamp;
    public MeasurementDistance MinDistance => new(_aggregatedMeasurement.MinDistanceMm, _accountSensor);
    public MeasurementDistance MeanDistance => new(_aggregatedMeasurement.MeanDistanceMm, _accountSensor);
    public MeasurementDistance MaxDistance => new(_aggregatedMeasurement.MaxDistanceMm, _accountSensor);
    public MeasurementDistance LastDistance => new(_aggregatedMeasurement.LastDistanceMm, _accountSensor);
    public double BatV => _aggregatedMeasurement.BatV;
    public double RssiDbm => _aggregatedMeasurement.RssiDbm;
    public double RssiPrc => (_aggregatedMeasurement.RssiDbm + 150.0) / 60.0 * 80.0;
    public double BatteryPrc => (_aggregatedMeasurement.BatV - 3.0) / 0.335 * 100.0;
}
