using Core.Util;
using Microsoft.AspNetCore.Mvc;

namespace Site.ViewComponents;

public class MoistureViewComponent : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(
        MeasurementMoistureEx measurementMoistureEx)
    {
        return await Task.FromResult(View(measurementMoistureEx));
    }
}