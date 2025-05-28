using Core.Util;
using Microsoft.AspNetCore.Mvc;
using Site.Utilities;
using AccountSensor = Core.Entities.AccountSensor;

namespace Site.ViewComponents;

public class DiagramModel
{
    public required IMeasurementEx? MeasurementEx { get; init; }
}

public class DiagramViewComponent : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(
        IMeasurementEx? measurementEx)
    {
        var model = new DiagramModel
        {
            MeasurementEx = measurementEx
        };
        return await Task.FromResult(View(model));
    }
}