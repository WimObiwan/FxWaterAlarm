using Microsoft.AspNetCore.Mvc;
using Site.Pages;

namespace Site.ViewComponents;

public class MeasurementsGraphViewComponent : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(
        MeasurementAggEx[] measurements)
    {
        return await Task.FromResult(View(measurements));
    }
}