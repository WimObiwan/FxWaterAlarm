using Core.Entities;
using Core.Util;

namespace Site.Utilities;

public class TrendMeasurementEx
{
    private readonly TimeSpan _timeSpan;
    private readonly MeasurementLevelEx _measurementLevelEx;
    private readonly MeasurementLevelEx _trendMeasurementEx;

    public TrendMeasurementEx(TimeSpan timeSpan, MeasurementLevelEx trend, MeasurementLevelEx measurementLevelEx)
    {
        _timeSpan = timeSpan;
        _measurementLevelEx = measurementLevelEx;
        _trendMeasurementEx = trend;
    }

    public double? DifferenceWaterL => 
        _measurementLevelEx.Distance.WaterL.HasValue && _trendMeasurementEx.Distance.WaterL.HasValue 
            ? _measurementLevelEx.Distance.WaterL.Value - _trendMeasurementEx.Distance.WaterL.Value
            : null;

    public double? DifferenceWaterLPerDay =>
        DifferenceWaterL.HasValue ? DifferenceWaterL.Value / _timeSpan.TotalDays : null;

    public double? DifferenceLevelFraction => 
        _measurementLevelEx.Distance.LevelFraction.HasValue && _trendMeasurementEx.Distance.LevelFraction.HasValue 
            ? _measurementLevelEx.Distance.LevelFraction.Value - _trendMeasurementEx.Distance.LevelFraction.Value
            : null;

    public double? DifferenceLevelFractionPerDay =>
        DifferenceLevelFraction.HasValue ? DifferenceLevelFraction.Value / _timeSpan.TotalDays : null;

    public double? DifferenceHeight => 
        _measurementLevelEx.Distance.HeightMm.HasValue && _trendMeasurementEx.Distance.HeightMm.HasValue 
            ? _measurementLevelEx.Distance.HeightMm.Value - _trendMeasurementEx.Distance.HeightMm.Value
            : null;

    public double? DifferenceHeightPerDay =>
        DifferenceHeight.HasValue ? DifferenceHeight.Value / _timeSpan.TotalDays : null;

    public TimeSpan? TimeTillEmpty => 
        _measurementLevelEx.Distance.WaterL.HasValue && _trendMeasurementEx.Distance.WaterL.HasValue 
                                                && _measurementLevelEx.Distance.WaterL.Value < _trendMeasurementEx.Distance.WaterL.Value
            ? _measurementLevelEx.Distance.WaterL.Value / (_trendMeasurementEx.Distance.WaterL.Value - _measurementLevelEx.Distance.WaterL.Value) * _timeSpan
            : null;
    
    public TimeSpan? TimeTillFull => 
        _measurementLevelEx.Distance.WaterL.HasValue && _trendMeasurementEx.Distance.WaterL.HasValue 
                                                && _measurementLevelEx.Distance.WaterL.Value > _trendMeasurementEx.Distance.WaterL.Value
            ? (_measurementLevelEx.AccountSensor.CapacityL - _measurementLevelEx.Distance.WaterL.Value) / (_measurementLevelEx.Distance.WaterL.Value - _trendMeasurementEx.Distance.WaterL.Value) * _timeSpan
            : null;
}
