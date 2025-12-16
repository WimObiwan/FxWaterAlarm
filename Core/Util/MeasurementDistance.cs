using Core.Entities;

namespace Core.Util;

public class MeasurementDistance
{
    private readonly Core.Entities.AccountSensor _accountSensor;
    
    public MeasurementDistance(int? distanceMm, Core.Entities.AccountSensor accountSensor)
    {
        DistanceMm = distanceMm;
        _accountSensor = accountSensor;
    }

    public int? DistanceMm { get; }

    public int? HeightMm
    {
        get
        {
            if (!DistanceMm.HasValue)
                return null;
                
            if (_accountSensor.Sensor.Type == SensorType.Level)
            {
                if (_accountSensor is { DistanceMmEmpty: not null })
                    return _accountSensor.DistanceMmEmpty.Value - DistanceMm.Value;
            }
            else if (_accountSensor.Sensor.Type == SensorType.LevelPressure)
            {
                return DistanceMm.Value + (_accountSensor.DistanceMmEmpty ?? 0);
            }

            return null;
        }
    }

    public double? RealLevelFraction
    {
        get
        {
            if (!DistanceMm.HasValue)
                return null;

            if (_accountSensor.Sensor.Type == SensorType.Level)
            {
                if (_accountSensor is { DistanceMmEmpty: not null, DistanceMmFull: not null })
                {
                    return ((double)_accountSensor.DistanceMmEmpty.Value - DistanceMm.Value - (_accountSensor.UnusableHeightMm ?? 0))
                           / ((double)_accountSensor.DistanceMmEmpty.Value - _accountSensor.DistanceMmFull.Value - (_accountSensor.UnusableHeightMm ?? 0));
                }
            }
            else if (_accountSensor.Sensor.Type == SensorType.LevelPressure)
            {
                if (_accountSensor is { DistanceMmFull: not null })
                {
                    return (((double)(_accountSensor.DistanceMmEmpty ?? 0)) + DistanceMm.Value - (_accountSensor.UnusableHeightMm ?? 0))
                           / (((double)(_accountSensor.DistanceMmEmpty ?? 0)) + _accountSensor.DistanceMmFull.Value - (_accountSensor.UnusableHeightMm ?? 0));
                }
            }

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
            if (!_accountSensor.NoMinMaxConstraints)
            {
                if (realLevelFraction.Value > 1.0)
                    return 1.0;
                if (realLevelFraction.Value < 0.0)
                    return 0.0;
            }
            return realLevelFraction;
        }
    }

    public double? RealLevelFractionIncludingUnusableHeight
    {
        get
        {
            if (!DistanceMm.HasValue)
                return null;

            if (_accountSensor.Sensor.Type == SensorType.Level)
            {
                if (_accountSensor is { DistanceMmEmpty: not null, DistanceMmFull: not null })
                {
                    return ((double)_accountSensor.DistanceMmEmpty.Value - DistanceMm.Value)
                           / ((double)_accountSensor.DistanceMmEmpty.Value - _accountSensor.DistanceMmFull.Value);
                }
            }
            else if (_accountSensor.Sensor.Type == SensorType.LevelPressure)
            {
                if (_accountSensor is { DistanceMmFull: not null })
                {
                    return (((double)(_accountSensor.DistanceMmEmpty ?? 0)) + DistanceMm.Value)
                           / (((double)(_accountSensor.DistanceMmEmpty ?? 0)) + _accountSensor.DistanceMmFull.Value);
                }
            }

            return null;
        }
    }

    public double? LevelFractionIncludingUnusableHeight
    {
        get
        {
            var realLevelFraction = RealLevelFractionIncludingUnusableHeight;
            if (!realLevelFraction.HasValue)
                return null;
            if (!_accountSensor.NoMinMaxConstraints)
            {
                if (realLevelFraction.Value > 1.0)
                    return 1.0;
                if (realLevelFraction.Value < 0.0)
                    return 0.0;
            }
            return realLevelFraction;
        }
    }

    public double? WaterL
    {
        get
        {
            var levelFraction = LevelFraction;
            if (levelFraction != null && _accountSensor.UsableCapacityL.HasValue)
                return levelFraction.Value * _accountSensor.UsableCapacityL.Value;

            return null;
        }
    }
}
