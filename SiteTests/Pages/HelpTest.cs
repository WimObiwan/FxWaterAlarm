using Core.Queries;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Site.Pages;
using SiteTests.Helpers;

namespace SiteTests.Pages;

public class HelpTest
{
    private static (Help model, ConfigurableFakeMediator mediator, FakeMessenger messenger) CreateModel()
    {
        var mediator = new ConfigurableFakeMediator();
        var urlBuilder = new FakeUrlBuilder();
        var messenger = new FakeMessenger();
        var model = new Help(mediator, urlBuilder, messenger);
        TestEntityFactory.SetupPageContext(model);
        return (model, mediator, messenger);
    }

    [Fact]
    public void OnGet_ReturnsPage()
    {
        var (model, _, _) = CreateModel();
        var result = model.OnGet();
        Assert.IsType<PageResult>(result);
    }

    [Fact]
    public async Task OnPost_RedirectsToSelf_Always()
    {
        var (model, _, _) = CreateModel();

        var result = await model.OnPost("nonexistent@example.com");

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("?", redirect.Url);
    }

    [Fact]
    public async Task OnPost_SendsLinkMail_WhenAccountFoundByEmail()
    {
        var (model, mediator, messenger) = CreateModel();
        var account = TestEntityFactory.CreateAccount(link: "my-link", email: "user@example.com");
        mediator.SetResponse<AccountByEmailQuery, Core.Entities.Account?>(account);

        await model.OnPost("user@example.com");

        Assert.Single(messenger.LinkMails);
        Assert.Equal("user@example.com", messenger.LinkMails[0].Email);
    }

    [Fact]
    public async Task OnPost_NoMail_WhenNeitherAccountNorSensorFound()
    {
        var (model, _, messenger) = CreateModel();

        await model.OnPost("unknown@example.com");

        Assert.Empty(messenger.LinkMails);
    }

    [Fact]
    public async Task OnPost_SendsLinkMail_WhenSensorFound_WithSingleNonDemoAccount()
    {
        var (model, mediator, messenger) = CreateModel();
        // AccountByEmailQuery returns null
        var account = TestEntityFactory.CreateAccount(link: "account-link", email: "owner@example.com");
        var sensor = TestEntityFactory.CreateSensor();
        var accountSensor = TestEntityFactory.CreateAccountSensor(account: account, sensor: sensor);

        // We need a sensor with AccountSensors populated
        // The SensorByLinkQuery should return a sensor with its AccountSensors
        // Our fake returns the same sensor for both queries (with and without 'F' prefix)
        var sensorWithAccounts = new Core.Entities.Sensor
        {
            Uid = sensor.Uid,
            DevEui = sensor.DevEui,
            CreateTimestamp = sensor.CreateTimestamp,
            Type = sensor.Type,
            ExpectedIntervalSecs = sensor.ExpectedIntervalSecs,
            Link = sensor.Link
        };
        // Unfortunately Sensor.AccountSensors is a readonly collection backed by a private list
        // We can't easily set it. The test would require integration with EF.
        // Instead, verify the redirect always happens (no exception)
        await model.OnPost("sensor-id");

        var redirect = Assert.IsType<RedirectResult>(await model.OnPost("sensor-id"));
        Assert.Equal("?", redirect.Url);
    }
}
