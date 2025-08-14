using Core.Util;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Site.Utilities;

namespace Site.ViewComponents;

public class DetectViewComponent : ViewComponent
{
    private readonly MeasurementDisplayOptions _options;

    public DetectViewComponent(IOptions<MeasurementDisplayOptions> options)
    {
        _options = options.Value;
    }

    public async Task<IViewComponentResult> InvokeAsync(
        MeasurementDetectEx measurementDetectEx)
    {
        var model = new MeasurementDisplayModel<MeasurementDetectEx>
        {
            Measurement = measurementDetectEx,
            IsOldMeasurement = measurementDetectEx.IsOld(_options.OldMeasurementThreshold)
        };
        
        return await Task.FromResult(View(model));
    }
}