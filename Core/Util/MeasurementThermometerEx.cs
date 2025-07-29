using Core.Entities;

namespace Core.Util;

public class MeasurementThermometerEx : MeasurementEx<MeasurementThermometer>
{
    public MeasurementThermometerEx(MeasurementThermometer measurement, AccountSensor accountSensor)
        : base(measurement, accountSensor)
    {
    }

    public double TempC => Measurement.TempC;
    public double HumPrc => Measurement.HumPrc;
}
