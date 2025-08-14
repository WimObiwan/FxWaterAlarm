using Core.Util;

namespace Site.ViewComponents;

public class MeasurementDisplayModel<T> where T : IMeasurementEx
{
    public required T Measurement { get; init; }
    public required bool IsOldMeasurement { get; init; }
}