using Microsoft.AspNetCore.Mvc;
using Site.Pages;
using AccountSensor = Core.Entities.AccountSensor;

namespace Site.ViewComponents;

public class SensorSettingsModel
{
    public required AccountSensor AccountSensor { get; init; }
    public required string Url { get; init; }
    public string LoginUrl => AccountLoginMessage.GetUrl(1, AccountSensor.Account.Link, Url);
    public Pages.AccountSensor.SaveResultEnum SaveResult { get; init; }
}

public class SensorSettingsViewComponent : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(
        AccountSensor accountSensor, string url, Pages.AccountSensor.SaveResultEnum saveResult)
    {
        var model = new SensorSettingsModel
        {
            AccountSensor = accountSensor,
            Url = url,
            SaveResult = saveResult
        };
        return await Task.FromResult(View(model));
    }
}