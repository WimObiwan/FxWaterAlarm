using Core.Util;
using Microsoft.AspNetCore.Mvc;

namespace Site.ViewComponents;

public class ThermometerViewComponent : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(
        MeasurementThermometerEx measurementThermometerEx)
    {
        return await Task.FromResult(View(measurementThermometerEx));
    }
}