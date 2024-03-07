using Microsoft.AspNetCore.Mvc;
using Site.Pages;

namespace Site.ViewComponents;

public class MeasurementsGraphModel
{
    public required Core.Entities.AccountSensor? AccountSensorEntity { get; init; }
    public required MeasurementAggEx[] Measurements { get; init; }
}

public class MeasurementsGraphViewComponent : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(
        Core.Entities.AccountSensor? accountSensorEntity,
        MeasurementAggEx[] measurements)
    {
        MeasurementsGraphModel model = new()
        {
            AccountSensorEntity = accountSensorEntity,
            Measurements = measurements
        };
        return await Task.FromResult(View(model));
    }
}