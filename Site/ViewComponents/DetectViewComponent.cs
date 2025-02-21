using Core.Util;
using Microsoft.AspNetCore.Mvc;

namespace Site.ViewComponents;

public class DetectViewComponent : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(
        MeasurementDetectEx measurementDetectEx)
    {
        return await Task.FromResult(View(measurementDetectEx));
    }
}