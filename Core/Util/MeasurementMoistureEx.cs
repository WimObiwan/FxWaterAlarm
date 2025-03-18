using Core.Entities;

namespace Core.Util;

public class MeasurementMoistureEx : MeasurementEx<MeasurementMoisture>
{
    public MeasurementMoistureEx(MeasurementMoisture measurement, AccountSensor accountSensor)
        : base(measurement, accountSensor)
    {
    }

    public double SoilMoisturePrc => Measurement.SoilMoisturePrc;
    public double SoilConductivity => Measurement.SoilConductivity;
    public double SoilTemperatureC => Measurement.SoilTemperature;
}
