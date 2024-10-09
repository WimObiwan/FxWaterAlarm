using Core.Util;
using Microsoft.AspNetCore.Mvc;
using AccountSensor = Core.Entities.AccountSensor;

namespace Site.ViewComponents;

public class MeasurementDetailsModel
{
    public required MeasurementLevelEx? MeasurementLevelEx { get; init; }
    public required AccountSensor AccountSensor { get; init; }
}

public class MeasurementDetailsViewComponent : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(
        AccountSensor accountSensor, MeasurementLevelEx? measurementLevelEx)
    {
        var model = new MeasurementDetailsModel
        {
            MeasurementLevelEx = measurementLevelEx,
            AccountSensor = accountSensor
        };
        return await Task.FromResult(View(model));
    }
}