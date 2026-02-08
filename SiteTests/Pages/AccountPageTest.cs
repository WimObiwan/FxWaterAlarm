using Core.Commands;
using Core.Queries;
using Microsoft.AspNetCore.Mvc;
using Site.Pages;
using SiteTests.Helpers;

namespace SiteTests.Pages;

public class AccountPageTest
{
    private static (Account model, ConfigurableFakeMediator mediator, FakeUserInfo userInfo) CreateModel()
    {
        var mediator = new ConfigurableFakeMediator();
        var userInfo = new FakeUserInfo();
        var model = new Account(mediator, userInfo);
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
