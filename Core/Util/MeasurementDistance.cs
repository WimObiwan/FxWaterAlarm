using Core.Entities;

namespace Core.Util;

public class MeasurementDistance
{
    private const double ManholeResolutionLPerMm = 1.0; // 1 m² area = 1,000,000 mm² = 1 L/mm
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
            
            // Apply manhole compensation when NoMinMaxConstraints is true and level exceeds 100%
            if (_accountSensor.NoMinMaxConstraints && realLevelFraction.Value > 1.0)
            {
                return ApplyManholeCompensationToFraction(realLevelFraction.Value);
            }
            
            return realLevelFraction;
        }
    }

    public double? RealLevelFractionIncludingUnusableHeight
    {
        get
        {
            if (DistanceMm.HasValue && _accountSensor is { DistanceMmEmpty: not null, DistanceMmFull: not null })
            {
                return ((double)_accountSensor.DistanceMmEmpty.Value - DistanceMm.Value)
                       / ((double)_accountSensor.DistanceMmEmpty.Value - _accountSensor.DistanceMmFull.Value);
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
            
            // Apply manhole compensation when NoMinMaxConstraints is true and level exceeds 100%
            if (_accountSensor.NoMinMaxConstraints && realLevelFraction.Value > 1.0)
            {
                return ApplyManholeCompensationToFractionIncludingUnusableHeight(realLevelFraction.Value);
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

    private double? ApplyManholeCompensationToFraction(double realLevelFraction)
    {
        return ApplyManholeCompensation(realLevelFraction, _accountSensor.UsableCapacityL, _accountSensor.ResolutionL);
    }

    private double? ApplyManholeCompensationToFractionIncludingUnusableHeight(double realLevelFraction)
    {
        return ApplyManholeCompensation(realLevelFraction, _accountSensor.CapacityL, _accountSensor.ResolutionL);
    }

    private double? ApplyManholeCompensation(double realLevelFraction, double? capacityL, double? resolutionL)
    {
        // When level exceeds 100%, calculate the actual water volume considering manhole
        if (!capacityL.HasValue || !resolutionL.HasValue)
            return realLevelFraction;
        
        // Defensive check: only apply compensation for overflow scenarios
        if (realLevelFraction <= 1.0)
            return realLevelFraction;
        
        // Calculate the height in mm
        var heightMm = (capacityL.Value / resolutionL.Value);
        
        // Overflow height above 100%
        var overflowFraction = realLevelFraction - 1.0;
        var overflowHeightMm = overflowFraction * heightMm;
        
        // Volume in manhole (overflow * manhole resolution)
        var manholeVolumeL = overflowHeightMm * ManholeResolutionLPerMm;
        
        // Total volume = full well + manhole
        var totalVolumeL = capacityL.Value + manholeVolumeL;
        
        // Return as equivalent fraction
        return totalVolumeL / capacityL.Value;
    }
}
