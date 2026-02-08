using Core.Commands;
using Core.Entities;
using Core.Queries;
using Core.Util;
using Microsoft.AspNetCore.Mvc;
using Site.Pages;
using SiteTests.Helpers;
using AccountPage = Site.Pages.Account;

namespace SiteTests.Pages;

public class AccountPageTest
{
    private static (AccountPage model, ConfigurableFakeMediator mediator, FakeUserInfo userInfo) CreateModel()
    {
        var mediator = new ConfigurableFakeMediator();
        var userInfo = new FakeUserInfo();
        var model = new AccountPage(mediator, userInfo);
        TestEntityFactory.SetupPageContext(model);
        return (model, mediator, userInfo);
    }

    [Fact]
    public async Task OnGet_LoadsAccount_WhenFound()
    {
        var (model, mediator, _) = CreateModel();
        var account = TestEntityFactory.CreateAccount();
        mediator.SetResponse<AccountByLinkQuery, Core.Entities.Account?>(account);

        await model.OnGet("test-link");

        Assert.NotNull(model.AccountEntity);
        Assert.Equal(account.Email, model.AccountEntity.Email);
    }

    [Fact]
    public async Task OnGet_AccountEntityIsNull_WhenNotFound()
    {
        var (model, _, _) = CreateModel();

        await model.OnGet("nonexistent");

        Assert.Null(model.AccountEntity);
    }

    [Fact]
    public async Task OnGet_SetsMessage()
    {
        var (model, _, _) = CreateModel();

        await model.OnGet("link", "hello world");

        Assert.Equal("hello world", model.Message);
    }

    [Fact]
    public async Task OnGet_LoadsAccountSensorsWithMeasurements()
    {
        var (model, mediator, _) = CreateModel();
        // Create an account with AccountSensors populated
        var account = TestEntityFactory.CreateAccount();
        var sensor = TestEntityFactory.CreateSensor();
        var accountSensor = TestEntityFactory.CreateAccountSensor(account: account, sensor: sensor);

        // Add the accountSensor to the account's backing field
        var field = typeof(Core.Entities.Account).GetField("_accountSensors",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var list = (List<Core.Entities.AccountSensor>)field.GetValue(account)!;
        list.Add(accountSensor);

        mediator.SetResponse<AccountByLinkQuery, Core.Entities.Account?>(account);

        // Return a measurement for LastMeasurementQuery
        var measurement = TestEntityFactory.CreateMeasurementLevelEx(accountSensor);
        mediator.SetResponse<LastMeasurementQuery, IMeasurementEx?>(measurement);

        await model.OnGet("test-link");

        Assert.NotNull(model.AccountSensors);
        Assert.Single(model.AccountSensors);
        Assert.NotNull(model.AccountSensors[0].Item2); // measurement not null
    }

    [Fact]
    public async Task OnGet_AccountSensorsEmpty_WhenAccountHasNone()
    {
        var (model, mediator, _) = CreateModel();
        var account = TestEntityFactory.CreateAccount(); // empty AccountSensors list
        mediator.SetResponse<AccountByLinkQuery, Core.Entities.Account?>(account);

        await model.OnGet("test-link");

        Assert.NotNull(model.AccountSensors);
        Assert.Empty(model.AccountSensors);
    }

    [Fact]
    public async Task OnGet_AccountSensorsNull_WhenAccountNotFound()
    {
        var (model, _, _) = CreateModel();

        await model.OnGet("nonexistent");

        Assert.Null(model.AccountSensors);
    }

    [Fact]
    public async Task OnGet_MultipleAccountSensors_LoadsAll()
    {
        var (model, mediator, _) = CreateModel();
        var account = TestEntityFactory.CreateAccount();
        var sensor1 = TestEntityFactory.CreateSensor(devEui: "sensor-1");
        var sensor2 = TestEntityFactory.CreateSensor(devEui: "sensor-2");
        var as1 = TestEntityFactory.CreateAccountSensor(account: account, sensor: sensor1);
        var as2 = TestEntityFactory.CreateAccountSensor(account: account, sensor: sensor2);

        var field = typeof(Core.Entities.Account).GetField("_accountSensors",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var list = (List<Core.Entities.AccountSensor>)field.GetValue(account)!;
        list.Add(as1);
        list.Add(as2);

        mediator.SetResponse<AccountByLinkQuery, Core.Entities.Account?>(account);

        await model.OnGet("test-link");

        Assert.NotNull(model.AccountSensors);
        Assert.Equal(2, model.AccountSensors.Count);
    }

    [Fact]
    public async Task OnPostAddSensor_ReturnsNotFound_WhenAccountMissing()
    {
        var (model, _, _) = CreateModel();

        var result = await model.OnPostAddSensorAsync("nonexistent", "dev123");

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnPostAddSensor_ReturnsForbid_WhenNotAuthorized()
    {
        var (model, mediator, userInfo) = CreateModel();
        var account = TestEntityFactory.CreateAccount();
        mediator.SetResponse<AccountByLinkQuery, Core.Entities.Account?>(account);
        userInfo.CanUpdate = false;

        var result = await model.OnPostAddSensorAsync("test-link", "dev123");

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task OnPostAddSensor_RedirectsWithMessage_WhenSensorNotFound()
    {
        var (model, mediator, userInfo) = CreateModel();
        var account = TestEntityFactory.CreateAccount();
        mediator.SetResponse<AccountByLinkQuery, Core.Entities.Account?>(account);
        userInfo.CanUpdate = true;
        // SensorByLinkQuery returns null (no sensor registered)

        var result = await model.OnPostAddSensorAsync("test-link", "dev123");

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("Sensor not found", redirect.RouteValues?["message"]);
    }

    [Fact]
    public async Task OnPostAddSensor_AddsAndRedirects_WhenSensorFound()
    {
        var (model, mediator, userInfo) = CreateModel();
        var account = TestEntityFactory.CreateAccount();
        var sensor = TestEntityFactory.CreateSensor();
        mediator.SetResponse<AccountByLinkQuery, Core.Entities.Account?>(account);
        mediator.SetResponse<SensorByLinkQuery, Core.Entities.Sensor?>(sensor);
        userInfo.CanUpdate = true;

        var result = await model.OnPostAddSensorAsync("test-link", "dev123");

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("Sensor added successfully", redirect.RouteValues?["message"]);

        // Verify command was sent
        var cmd = mediator.SentRequests.OfType<AddSensorToAccountCommand>().Single();
        Assert.Equal(account.Uid, cmd.AccountUid);
        Assert.Equal(sensor.Uid, cmd.SensorUid);
    }
}
