using Core.Util;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Site.Services;
using Site.Utilities;

namespace Site.ViewComponents;

public class MoistureViewComponent : ViewComponent
{
    private readonly MeasurementDisplayOptions _options;

    public MoistureViewComponent(IOptions<MeasurementDisplayOptions> options)
    {
        _options = options.Value;
    }

    public async Task<IViewComponentResult> InvokeAsync(
        MeasurementMoistureEx measurementMoistureEx)
    {
        var model = new MeasurementDisplayModel<MeasurementMoistureEx>
        {
            Measurement = measurementMoistureEx,
            IsOldMeasurement = measurementMoistureEx.IsOld(_options.OldMeasurementThresholdIntervals)
        };
        
        return await Task.FromResult(View(model));
    }
}