using Microsoft.AspNetCore.Mvc;
using Site.Pages;

namespace Site.ViewComponents;

public class MeasurementDetailsModel
{
    public required MeasurementEx? MeasurementEx { get; init; }
    public required Core.Entities.AccountSensor AccountSensor { get; init; }
}

public class MeasurementDetailsViewComponent : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(
        Core.Entities.AccountSensor accountSensor, MeasurementEx? measurementEx)
    {
        var model = new MeasurementDetailsModel
        {
            MeasurementEx = measurementEx,
            AccountSensor = accountSensor
        };
        return await Task.FromResult(View(model));
    }
}