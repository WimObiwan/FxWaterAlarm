using Microsoft.AspNetCore.Mvc;
using Site.Pages;
using Site.Utilities;

namespace Site.ViewComponents;

public class MeasurementsGraphNewModel
{
    public required Core.Entities.AccountSensor? AccountSensorEntity { get; init; }
    public required bool ShowTimelineSlider { get; init; }
    public required int FromDays { get; init; }
}

public class MeasurementsGraphNewViewComponent : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(
        Core.Entities.AccountSensor? accountSensorEntity,
        bool showTimelineSlider,
        int fromDays)
    {
        MeasurementsGraphNewModel model = new()
        {
            AccountSensorEntity = accountSensorEntity,
            ShowTimelineSlider = showTimelineSlider,
            FromDays = fromDays,
        };
        return await Task.FromResult(View(model));
    }
}