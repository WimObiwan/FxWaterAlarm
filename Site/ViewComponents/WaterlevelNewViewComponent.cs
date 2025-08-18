using Core.Util;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Site.Utilities;

namespace Site.ViewComponents;

public class WaterlevelNewViewComponent : ViewComponent
{
    private readonly MeasurementDisplayOptions _options;

    public WaterlevelNewViewComponent(IOptions<MeasurementDisplayOptions> options)
    {
        _options = options.Value;
    }

    public async Task<IViewComponentResult> InvokeAsync(
        MeasurementLevelEx measurementLevelEx)
    {
        var model = new MeasurementDisplayModel<MeasurementLevelEx>
        {
            Measurement = measurementLevelEx,
            IsOldMeasurement = measurementLevelEx.IsOld(_options.OldMeasurementThresholdIntervals)
        };
        
        return await Task.FromResult(View(model));
    }
}