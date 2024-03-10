namespace Site.Utilities;

public class MeasurementDistance
{
    private readonly Core.Entities.AccountSensor _accountSensor;
    
    public MeasurementDistance(int? distanceMm, Core.Entities.AccountSensor accountSensor)
    {
        DistanceMm = distanceMm;
        _accountSensor = accountSensor;
    }

    public int? DistanceMm { get; }

    public int? Height
    {
        get
        {
            if (DistanceMm.HasValue && _accountSensor is { DistanceMmEmpty: not null })
                return _accountSensor.DistanceMmEmpty.Value - DistanceMm.Value;
            return null;
        }
    }

    public double? RealLevelFraction
    {
        get
        {
            if (DistanceMm.HasValue && _accountSensor is { DistanceMmEmpty: not null, DistanceMmFull: not null })
                return ((double)_accountSensor.DistanceMmEmpty.Value - DistanceMm.Value)
                       / ((double)_accountSensor.DistanceMmEmpty.Value - _accountSensor.DistanceMmFull.Value);
            return null;
        }
    }

    public double? LevelFraction
    {
        get
        {
            var realLevelFraction = RealLevelFraction;
            if (!realLevelFraction.HasValue)
                return null;
            if (realLevelFraction.Value > 1.0)
                return 1.0;
            if (realLevelFraction.Value < 0.0)
                return 0.0;
            return realLevelFraction;
        }
    }

    public double? WaterL
    {
        get
        {
            var levelFraction = LevelFraction;
            if (levelFraction != null && _accountSensor.CapacityL.HasValue)
                return levelFraction.Value * _accountSensor.CapacityL.Value;

            return null;
        }
    }
}
