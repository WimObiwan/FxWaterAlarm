using Core.Queries;
using Microsoft.AspNetCore.Mvc;
using Site.Pages;
using SiteTests.Helpers;

namespace SiteTests.Pages;

public class SensorPageTest
{
    [Fact]
    public async Task OnGet_RedirectsToRestPath_WhenFound()
    {
        var mediator = new ConfigurableFakeMediator();
        var accountSensor = TestEntityFactory.CreateAccountSensor();
        mediator.SetResponse<AccountSensorByLinkQuery, Core.Entities.AccountSensor?>(accountSensor);

        var model = new Sensor(mediator);
        TestEntityFactory.SetupPageContext(model);

        var result = await model.OnGet("test-sensor-link");

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal(accountSensor.RestPath, redirect.Url);
    }

    [Fact]
    public async Task OnGet_ReturnsNotFound_WhenSensorNotFound()
    {
        var mediator = new ConfigurableFakeMediator();
        // No response set → returns null
        var model = new Sensor(mediator);
        TestEntityFactory.SetupPageContext(model);

        var result = await model.OnGet("nonexistent");

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnGet_ReturnsNotFound_WhenRestPathIsNull()
    {
        var mediator = new ConfigurableFakeMediator();
        var account = TestEntityFactory.CreateAccount(link: null);
        var accountSensor = TestEntityFactory.CreateAccountSensor(account: account);
        mediator.SetResponse<AccountSensorByLinkQuery, Core.Entities.AccountSensor?>(accountSensor);

        var model = new Sensor(mediator);
        TestEntityFactory.SetupPageContext(model);

        var result = await model.OnGet("test");

        Assert.IsType<NotFoundResult>(result);
    }
}
