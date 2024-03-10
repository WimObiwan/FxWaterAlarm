using Microsoft.AspNetCore.Mvc;
using Site.Pages;
using Site.Utilities;

namespace Site.ViewComponents;

public class WaterlevelViewComponent : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(
        MeasurementEx measurementEx)
    {
        return await Task.FromResult(View(measurementEx));
    }
}