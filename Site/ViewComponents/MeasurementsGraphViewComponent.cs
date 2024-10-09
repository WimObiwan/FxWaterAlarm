using Microsoft.AspNetCore.Mvc;
using Site.Pages;
using Site.Utilities;

namespace Site.ViewComponents;

public class MeasurementsGraphModel
{
    public required Core.Entities.AccountSensor? AccountSensorEntity { get; init; }
    public required AggregatedMeasurementLevelEx[] Measurements { get; init; }
}

public class MeasurementsGraphViewComponent : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(
        Core.Entities.AccountSensor? accountSensorEntity,
        AggregatedMeasurementLevelEx[] measurements)
    {
        MeasurementsGraphModel model = new()
        {
            AccountSensorEntity = accountSensorEntity,
            Measurements = measurements
        };
        return await Task.FromResult(View(model));
    }
}