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

        // Populate sensor's _accountSensors via reflection
        var sensorWithAccounts = TestEntityFactory.CreateSensor(devEui: sensor.DevEui);
        var field = typeof(Core.Entities.Sensor).GetField("_accountSensors",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var list = new List<Core.Entities.AccountSensor> { accountSensor };
        field.SetValue(sensorWithAccounts, list);

        mediator.SetResponse<SensorByLinkQuery, Core.Entities.Sensor?>(sensorWithAccounts);

        await model.OnPost("sensor-id");

        Assert.Single(messenger.LinkMails);
        Assert.Equal("owner@example.com", messenger.LinkMails[0].Email);
    }

    [Fact]
    public async Task OnPost_SendsLinkMail_WithAccountRestPath_WhenSensorRestPathNull()
    {
        var (model, mediator, messenger) = CreateModel();
        // Account with a RestPath from account's own link
        var account = TestEntityFactory.CreateAccount(link: "account-link", email: "owner@example.com");
        // Sensor with null link → RestPath is null on AccountSensor
        var sensor = new Core.Entities.Sensor
        {
            Uid = Guid.NewGuid(),
            DevEui = "test-sensor",
            CreateTimestamp = DateTime.UtcNow,
            Type = Core.Entities.SensorType.Level,
            ExpectedIntervalSecs = 3600,
            Link = null // null link means RestPath will be null on AccountSensor
        };
        var accountSensor = TestEntityFactory.CreateAccountSensor(account: account, sensor: sensor);

        var sensorWithAccounts = new Core.Entities.Sensor
        {
            Uid = sensor.Uid,
            DevEui = sensor.DevEui,
            CreateTimestamp = sensor.CreateTimestamp,
            Type = sensor.Type,
            ExpectedIntervalSecs = sensor.ExpectedIntervalSecs,
            Link = null
        };
        var field = typeof(Core.Entities.Sensor).GetField("_accountSensors",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        field.SetValue(sensorWithAccounts, new List<Core.Entities.AccountSensor> { accountSensor });

        mediator.SetResponse<SensorByLinkQuery, Core.Entities.Sensor?>(sensorWithAccounts);

        await model.OnPost("sensor-id");

        // account.RestPath is used as fallback when accountSensor.RestPath is null
        Assert.Single(messenger.LinkMails);
        var mail = messenger.LinkMails[0];
        Assert.Equal("owner@example.com", mail.Email);
        Assert.Contains("/a/account-link", mail.Url);
    }

    [Fact]
    public async Task OnPost_SendsLinkMail_WhenSensorFoundById()
    {
        var (model, mediator, messenger) = CreateModel();
        // Note: ConfigurableFakeMediator returns the same response for any SensorByLinkQuery,
        // so we cannot actually test the F-prefixed fallback behavior with this setup.
        // This test verifies that a mail is sent when a sensor is found by ID.
        var account = TestEntityFactory.CreateAccount(link: "account-link", email: "owner@example.com");
        var sensor = TestEntityFactory.CreateSensor(devEui: "Fsensor123");
        var accountSensor = TestEntityFactory.CreateAccountSensor(account: account, sensor: sensor);

        var sensorWithAccounts = TestEntityFactory.CreateSensor(devEui: "Fsensor123");
        var field = typeof(Core.Entities.Sensor).GetField("_accountSensors",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        field.SetValue(sensorWithAccounts, new List<Core.Entities.AccountSensor> { accountSensor });

        mediator.SetResponse<SensorByLinkQuery, Core.Entities.Sensor?>(sensorWithAccounts);

        await model.OnPost("sensor123");

        Assert.Single(messenger.LinkMails);
    }

    [Fact]
    public async Task OnPost_AccountByEmail_SendsLinkMail_UsingAccountRestPath()
    {
        var (model, mediator, messenger) = CreateModel();
        var account = TestEntityFactory.CreateAccount(link: "my-link", email: "user@example.com");
        mediator.SetResponse<AccountByEmailQuery, Core.Entities.Account?>(account);

        await model.OnPost("user@example.com");

        Assert.Single(messenger.LinkMails);
        // The link should use account.RestPath which needs Account.Link to be non-null
        // account.Link = "my-link", so RestPath exists
        Assert.Contains("my-link", messenger.LinkMails[0].Url);
    }

    [Fact]
    public async Task OnPost_SensorWithMultipleNonDemoAccounts_NoLinkMail()
    {
        var (model, mediator, messenger) = CreateModel();
        // Multiple non-demo accounts → count != 1 → no restPath from sensor
        var account1 = TestEntityFactory.CreateAccount(link: "link-1", email: "a@test.com");
        var account2 = TestEntityFactory.CreateAccount(link: "link-2", email: "b@test.com");
        var sensor = TestEntityFactory.CreateSensor();
        var as1 = TestEntityFactory.CreateAccountSensor(account: account1, sensor: sensor);
        var as2 = TestEntityFactory.CreateAccountSensor(account: account2, sensor: sensor);

        var sensorWithAccounts = TestEntityFactory.CreateSensor(devEui: sensor.DevEui);
        var field = typeof(Core.Entities.Sensor).GetField("_accountSensors",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        field.SetValue(sensorWithAccounts, new List<Core.Entities.AccountSensor> { as1, as2 });

        mediator.SetResponse<SensorByLinkQuery, Core.Entities.Sensor?>(sensorWithAccounts);

        await model.OnPost("sensor-id");

        // Multiple non-demo accounts → accountSensors.Count() != 1 → account is set to null from sensor path
        // Then account is still null → no mail sent
        Assert.Empty(messenger.LinkMails);
    }

    [Fact]
    public async Task OnPost_SensorWithOnlyDemoAccounts_NoRestPathOrMail()
    {
        var (model, mediator, messenger) = CreateModel();
        var demoAccount = TestEntityFactory.CreateAccount(link: "demo-link", email: "demo@wateralarm.be", isDemo: true);
        var sensor = TestEntityFactory.CreateSensor();
        var accountSensor = TestEntityFactory.CreateAccountSensor(account: demoAccount, sensor: sensor);

        var sensorWithAccounts = TestEntityFactory.CreateSensor(devEui: sensor.DevEui);
        var field = typeof(Core.Entities.Sensor).GetField("_accountSensors",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        field.SetValue(sensorWithAccounts, new List<Core.Entities.AccountSensor> { accountSensor });

        mediator.SetResponse<SensorByLinkQuery, Core.Entities.Sensor?>(sensorWithAccounts);

        await model.OnPost("sensor-id");

        // Demo account is filtered out → accountSensors is empty → Count != 1 → no account from sensor path
        // account is null → no mail
        Assert.Empty(messenger.LinkMails);
    }
}
