using Core.Entities;

namespace Site.Utilities;

public class TrendMeasurementEx
{
    private readonly TimeSpan _timeSpan;
    private readonly MeasurementEx _measurementEx;
    private readonly MeasurementEx _trendMeasurementEx;

    public TrendMeasurementEx(TimeSpan timeSpan, Measurement trend, MeasurementEx measurementEx)
    {
        _timeSpan = timeSpan;
        _measurementEx = measurementEx;
        _trendMeasurementEx = new MeasurementEx(trend, measurementEx.AccountSensor);
    }

    public double? DifferenceWaterL => 
        _measurementEx.Distance.WaterL.HasValue && _trendMeasurementEx.Distance.WaterL.HasValue 
            ? _measurementEx.Distance.WaterL.Value - _trendMeasurementEx.Distance.WaterL.Value
            : null;

    public double? DifferenceWaterLPerDay =>
        DifferenceWaterL.HasValue ? DifferenceWaterL.Value / _timeSpan.TotalDays : null;

    public double? DifferenceLevelFraction => 
        _measurementEx.Distance.LevelFraction.HasValue && _trendMeasurementEx.Distance.LevelFraction.HasValue 
            ? _measurementEx.Distance.LevelFraction.Value - _trendMeasurementEx.Distance.LevelFraction.Value
            : null;

    public double? DifferenceLevelFractionPerDay =>
        DifferenceLevelFraction.HasValue ? DifferenceLevelFraction.Value / _timeSpan.TotalDays : null;

    public double? DifferenceHeight => 
        _measurementEx.Distance.Height.HasValue && _trendMeasurementEx.Distance.Height.HasValue 
            ? _measurementEx.Distance.Height.Value - _trendMeasurementEx.Distance.Height.Value
            : null;

    public double? DifferenceHeightPerDay =>
        DifferenceHeight.HasValue ? DifferenceHeight.Value / _timeSpan.TotalDays : null;

    public TimeSpan? TimeTillEmpty => 
        _measurementEx.Distance.WaterL.HasValue && _trendMeasurementEx.Distance.WaterL.HasValue 
                                                && _measurementEx.Distance.WaterL.Value < _trendMeasurementEx.Distance.WaterL.Value
            ? _measurementEx.Distance.WaterL.Value / (_trendMeasurementEx.Distance.WaterL.Value - _measurementEx.Distance.WaterL.Value) * _timeSpan
            : null;
    
    public TimeSpan? TimeTillFull => 
        _measurementEx.Distance.WaterL.HasValue && _trendMeasurementEx.Distance.WaterL.HasValue 
                                                && _measurementEx.Distance.WaterL.Value > _trendMeasurementEx.Distance.WaterL.Value
            ? (_measurementEx.AccountSensor.CapacityL - _measurementEx.Distance.WaterL.Value) / (_measurementEx.Distance.WaterL.Value - _trendMeasurementEx.Distance.WaterL.Value) * _timeSpan
            : null;
}
