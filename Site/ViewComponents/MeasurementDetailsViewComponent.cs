using Core.Util;
using Microsoft.AspNetCore.Mvc;
using AccountSensor = Core.Entities.AccountSensor;

namespace Site.ViewComponents;

public class MeasurementDetailsModel
{
    public required IMeasurementEx? MeasurementEx { get; init; }
    public required AccountSensor AccountSensor { get; init; }
}

public class MeasurementDetailsViewComponent : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(
        AccountSensor accountSensor, IMeasurementEx? measurementEx)
    {
        var model = new MeasurementDetailsModel
        {
            MeasurementEx = measurementEx,
            AccountSensor = accountSensor
        };
        return await Task.FromResult(View(model));
    }
}