using Core.Util;
using Microsoft.AspNetCore.Mvc;

namespace Site.ViewComponents;

public class WaterlevelViewComponent : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(
        MeasurementLevelEx measurementLevelEx)
    {
        return await Task.FromResult(View(measurementLevelEx));
    }
}