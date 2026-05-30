using Core.Entities;

namespace Core.Util;

public class MeasurementDistance
{
    private const double DefaultDensityKgPerM3 = 1000.0;
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
                var adjustedDistance = GetDensityAdjustedPressureDistanceMm();
                return (int)Math.Round(adjustedDistance + (_accountSensor.DistanceMmEmpty ?? 0), MidpointRounding.AwayFromZero);
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
                    return (((double)(_accountSensor.DistanceMmEmpty ?? 0)) + GetDensityAdjustedPressureDistanceMm() - (_accountSensor.UnusableHeightMm ?? 0))
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
            if (TryGetHorizontalCylinderLevelFraction(out var horizontalCylinderLevelFraction))
                return horizontalCylinderLevelFraction;

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
                    return (((double)(_accountSensor.DistanceMmEmpty ?? 0)) + GetDensityAdjustedPressureDistanceMm())
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
            if (TryGetHorizontalCylinderVolumeL(out var horizontalCylinderVolumeL))
                return horizontalCylinderVolumeL;

            var levelFraction = LevelFraction;
            if (levelFraction != null && _accountSensor.UsableCapacityL.HasValue)
                return levelFraction.Value * _accountSensor.UsableCapacityL.Value;

            return null;
        }
    }

    private double GetEffectiveDensityKgPerM3()
    {
        if (_accountSensor.DensityKgPerM3 is { } density && density > 0)
            return density;

        return DefaultDensityKgPerM3;
    }

    private double GetDensityAdjustedPressureDistanceMm()
    {
        if (!DistanceMm.HasValue)
            return 0;

        return DistanceMm.Value * (DefaultDensityKgPerM3 / GetEffectiveDensityKgPerM3());
    }

    private bool TryGetHorizontalCylinderVolumeL(out double volumeL)
    {
        volumeL = 0.0;

        if (_accountSensor.Geometry != TankGeometry.HorizontalCylinder)
            return false;

        if (_accountSensor.CapacityL is not { } capacityL)
            return false;

        var diameterMm = GetConfiguredCylinderDiameterMm();
        var liquidHeightMm = GetHorizontalCylinderLiquidHeightMm(diameterMm);

        if (diameterMm <= 0 || capacityL <= 0 || !liquidHeightMm.HasValue)
            return false;

        var radiusMm = diameterMm / 2.0;
        var fullSectionAreaMm2 = Math.PI * radiusMm * radiusMm;
        if (fullSectionAreaMm2 <= 0.0)
            return false;

        // Length is deduced from known full capacity and diameter.
        var lengthMm = capacityL * 1_000_000.0 / fullSectionAreaMm2;
        var clampedHeightMm = Math.Clamp(liquidHeightMm.Value, 0.0, diameterMm);

        var segmentAreaMm2 = radiusMm * radiusMm * Math.Acos((radiusMm - clampedHeightMm) / radiusMm)
                             - (radiusMm - clampedHeightMm) * Math.Sqrt(Math.Max(0.0, 2.0 * radiusMm * clampedHeightMm - clampedHeightMm * clampedHeightMm));
        var volumeMm3 = lengthMm * segmentAreaMm2;
        var geometricVolumeL = volumeMm3 / 1_000_000.0;

        volumeL = Math.Clamp(geometricVolumeL, 0.0, capacityL);
        return true;
    }

    private double? GetHorizontalCylinderLiquidHeightMm(int diameterMm)
    {
        if (diameterMm <= 0 || !DistanceMm.HasValue)
            return null;

        return _accountSensor.Sensor.Type switch
        {
            SensorType.Level => HeightMm,
            SensorType.LevelPressure => _accountSensor.DistanceMmFull is { } distanceMmFull && distanceMmFull > 0
                ? Math.Clamp(GetDensityAdjustedPressureDistanceMm() / distanceMmFull * diameterMm, 0.0, diameterMm)
                : null,
            _ => null,
        };
    }

    private int GetConfiguredCylinderDiameterMm()
    {
        return _accountSensor.Sensor.Type switch
        {
            SensorType.LevelPressure => (_accountSensor.DistanceMmFull ?? 0) + (_accountSensor.DistanceMmEmpty ?? 0),
            SensorType.Level => (_accountSensor.DistanceMmEmpty ?? 0) - (_accountSensor.DistanceMmFull ?? 0),
            _ => 0,
        };
    }

    private bool TryGetHorizontalCylinderLevelFraction(out double levelFraction)
    {
        levelFraction = 0.0;

        if (!TryGetHorizontalCylinderVolumeL(out var volumeL))
            return false;

        if (_accountSensor.CapacityL is not { } capacityL || capacityL <= 0)
            return false;

        levelFraction = Math.Clamp(volumeL / capacityL, 0.0, 1.0);
        return true;
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
            return 1.0;
        
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
