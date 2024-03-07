using Microsoft.AspNetCore.Mvc;
using Site.Pages;
using AccountSensor = Core.Entities.AccountSensor;

namespace Site.ViewComponents;

public class TrendModel
{
    public required AccountSensor? AccountSensorEntity { get; init; }
    public required TrendMeasurementEx? TrendMeasurement1H { get; init; }
    public required TrendMeasurementEx? TrendMeasurement6H { get; init; }
    public required TrendMeasurementEx? TrendMeasurement24H { get; init; }
    public required TrendMeasurementEx? TrendMeasurement7D { get; init; }
    public required TrendMeasurementEx? TrendMeasurement30D { get; init; }
}

public class TrendViewComponent : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(
        AccountSensor? accountSensorEntity,
        TrendMeasurementEx? trendMeasurement1H,
        TrendMeasurementEx? trendMeasurement6H,
        TrendMeasurementEx? trendMeasurement24H,
        TrendMeasurementEx? trendMeasurement7D,
        TrendMeasurementEx? trendMeasurement30D)
    {
        var model = new TrendModel
        {
            AccountSensorEntity = accountSensorEntity,
            TrendMeasurement1H = trendMeasurement1H,
            TrendMeasurement6H = trendMeasurement6H,
            TrendMeasurement24H = trendMeasurement24H,
            TrendMeasurement7D = trendMeasurement7D,
            TrendMeasurement30D = trendMeasurement30D
        };
        return await Task.FromResult(View(model));
    }
}