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
            
            // Apply manhole compensation when level exceeds 100%
            if (realLevelFraction.Value > 1.0)
            {
                return ApplyManholeCompensationToFraction(realLevelFraction.Value);
            }
            
            // Clamp to 0.0 minimum
            if (realLevelFraction.Value < 0.0)
                return 0.0;
            
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
            
            // Apply manhole compensation when level exceeds 100%
            if (realLevelFraction.Value > 1.0)
            {
                return ApplyManholeCompensationToFractionIncludingUnusableHeight(realLevelFraction.Value);
            }
            
            // Clamp to 0.0 minimum
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
        
        // If ManholeAreaM2 is null or 0, don't apply manhole compensation (treat as no manhole)
        if (!_accountSensor.ManholeAreaM2.HasValue || _accountSensor.ManholeAreaM2.Value <= 0.0)
            return realLevelFraction;
        
        // Calculate the height in mm
        var heightMm = (capacityL.Value / resolutionL.Value);
        
        // Overflow height above 100%
        var overflowFraction = realLevelFraction - 1.0;
        var overflowHeightMm = overflowFraction * heightMm;
        
        // Calculate manhole resolution: ManholeAreaM2 (m²) * 1,000,000 (mm² per m²) / 1,000,000 (mm³ per L) = ManholeAreaM2 (L/mm)
        var manholeResolutionLPerMm = _accountSensor.ManholeAreaM2.Value;
        
        // Volume in manhole (overflow * manhole resolution)
        var manholeVolumeL = overflowHeightMm * manholeResolutionLPerMm;
        
        // Total volume = full well + manhole
        var totalVolumeL = capacityL.Value + manholeVolumeL;
        
        // Return as equivalent fraction
        return totalVolumeL / capacityL.Value;
    }
}
