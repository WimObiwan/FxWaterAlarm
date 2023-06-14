using Microsoft.AspNetCore.Mvc;
using Site.Pages;
using AccountSensor = Core.Entities.AccountSensor;

namespace Site.ViewComponents;

public class TrendModel
{
    // public required MeasurementEx? MeasurementEx { get; init; }
    // public required AccountSensor AccountSensor { get; init; }
}

public class TrendViewComponent : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(
        /*AccountSensor accountSensor, MeasurementEx? measurementEx*/)
    {
        var model = new TrendModel
        {
            // MeasurementEx = measurementEx,
            // AccountSensor = accountSensor
        };
        return await Task.FromResult(View(model));
    }
}