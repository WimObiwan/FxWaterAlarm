using Core.Util;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Site.Utilities;

namespace Site.ViewComponents;

public class ThermometerViewComponent : ViewComponent
{
    private readonly MeasurementDisplayOptions _options;

    public ThermometerViewComponent(IOptions<MeasurementDisplayOptions> options)
    {
        _options = options.Value;
    }

    public async Task<IViewComponentResult> InvokeAsync(
        MeasurementThermometerEx measurementThermometerEx)
    {
        var model = new MeasurementDisplayModel<MeasurementThermometerEx>
        {
            Measurement = measurementThermometerEx,
            IsOldMeasurement = measurementThermometerEx.IsOld(_options.OldMeasurementThresholdIntervals)
        };
        
        return await Task.FromResult(View(model));
    }
}