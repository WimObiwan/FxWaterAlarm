using Core.Util;
using Microsoft.AspNetCore.Mvc;

namespace Site.ViewComponents;

public class WaterlevelNewViewComponent : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(
        MeasurementEx measurementEx)
    {
        return await Task.FromResult(View(measurementEx));
    }
}