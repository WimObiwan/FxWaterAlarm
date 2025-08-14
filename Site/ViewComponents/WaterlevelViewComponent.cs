using Core.Util;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Site.Utilities;

namespace Site.ViewComponents;

public class WaterlevelViewComponent : ViewComponent
{
    private readonly MeasurementDisplayOptions _options;

    public WaterlevelViewComponent(IOptions<MeasurementDisplayOptions> options)
    {
        _options = options.Value;
    }

    public async Task<IViewComponentResult> InvokeAsync(
        MeasurementLevelEx measurementLevelEx)
    {
        var model = new MeasurementDisplayModel<MeasurementLevelEx>
        {
            Measurement = measurementLevelEx,
            IsOldMeasurement = measurementLevelEx.IsOld(_options.OldMeasurementThreshold)
        };
        
        return await Task.FromResult(View(model));
    }
}