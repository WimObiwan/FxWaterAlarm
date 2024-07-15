using Core.Entities;
using Microsoft.AspNetCore.Mvc;
using Site.Pages;
using Site.Utilities;

namespace Site.ViewComponents;

public class MeasurementsGraphNewModel
{
    public required Core.Entities.AccountSensor? AccountSensorEntity { get; init; }
    public required bool ShowTimelineSlider { get; init; }
    public required int FromDays { get; init; }
    public required GraphType GraphType { get; init; }
}

public class MeasurementsGraphNewViewComponent : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(
        Core.Entities.AccountSensor? accountSensorEntity,
        bool showTimelineSlider,
        int fromDays,
        GraphType graphType)
    {
        MeasurementsGraphNewModel model = new()
        {
            AccountSensorEntity = accountSensorEntity,
            ShowTimelineSlider = showTimelineSlider,
            FromDays = fromDays,
            GraphType = graphType
        };
        return await Task.FromResult(View(model));
    }
}