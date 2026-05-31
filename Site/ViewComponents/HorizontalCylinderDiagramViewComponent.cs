using Core.Util;
using Microsoft.AspNetCore.Mvc;

namespace Site.ViewComponents;

public class HorizontalCylinderDiagramModel
{
    public required MeasurementLevelEx MeasurementLevelEx { get; init; }
}

public class HorizontalCylinderDiagramViewComponent : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(MeasurementLevelEx measurementLevelEx)
    {
        var model = new HorizontalCylinderDiagramModel
        {
            MeasurementLevelEx = measurementLevelEx
        };

        return await Task.FromResult(View(model));
    }
}
