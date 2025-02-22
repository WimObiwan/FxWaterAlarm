using Core.Entities;

namespace Core.Util;

public class MeasurementLevelEx : MeasurementEx<MeasurementLevel>
{
    public MeasurementLevelEx(MeasurementLevel measurement, AccountSensor accountSensor)
        : base(measurement, accountSensor)
    {
    }

    public MeasurementDistance Distance => new(Measurement.DistanceMm, AccountSensor);
}
