using Core.Entities;

namespace Core.Util;

public class MeasurementDetectEx : MeasurementEx<MeasurementDetect>
{
    public MeasurementDetectEx(MeasurementDetect measurement, AccountSensor accountSensor)
        : base(measurement, accountSensor)
    {
    }

    public int Status => Measurement.Status;
}
