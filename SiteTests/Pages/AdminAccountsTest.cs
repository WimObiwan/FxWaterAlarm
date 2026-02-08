using Core.Commands;
using Core.Queries;
using Microsoft.AspNetCore.Mvc;
using Site.Pages;
using SiteTests.Helpers;

namespace SiteTests.Pages;

public class AdminAccountsTest
{
    private static (AdminAccounts model, ConfigurableFakeMediator mediator) CreateModel()
    {
        var mediator = new ConfigurableFakeMediator();
        var model = new AdminAccounts(mediator);
        TestEntityFactory.SetupPageContext(model);
        return (model, mediator);
    }

    [Fact]
    public async Task OnGet_SetsMessage()
    {
        var (model, mediator) = CreateModel();
        mediator.SetResponse<AccountsQuery, List<Core.Entities.Account>>(new List<Core.Entities.Account>());

        await model.OnGet("hello", null);

        Assert.Equal("hello", model.Message);
    }

    [Fact]
    public async Task OnGet_LoadsAccounts()
    {
        var (model, mediator) = CreateModel();
        var accounts = new List<Core.Entities.Account> { TestEntityFactory.CreateAccount() };
        mediator.SetResponse<AccountsQuery, List<Core.Entities.Account>>(accounts);

        await model.OnGet(null, null);

        Assert.Single(model.Accounts);
    }

    [Fact]
    public async Task OnPostAddAccount_CreatesAccountAndRedirects()
    {
        var (model, mediator) = CreateModel();
        var account = TestEntityFactory.CreateAccount(link: "new-link");
        mediator.SetResponse<AccountQuery, Core.Entities.Account?>(account);

        var result = await model.OnPostAddAccount("Test", "test@test.com");

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Contains("Account created successfully", Uri.UnescapeDataString(redirect.Url));
        // Verify CreateAccountCommand sent
        Assert.Contains(mediator.SentRequests, r => r is CreateAccountCommand);
    }

    [Fact]
    public async Task OnPostAddAccount_WhitespaceName_TreatedAsNull()
    {
        var (model, mediator) = CreateModel();
        var account = TestEntityFactory.CreateAccount();
        mediator.SetResponse<AccountQuery, Core.Entities.Account?>(account);

        await model.OnPostAddAccount("   ", "test@test.com");

        var cmd = mediator.SentRequests.OfType<CreateAccountCommand>().Single();
        Assert.Null(cmd.Name);
    }

    [Fact]
    public async Task OnPostAddAccount_ReturnsNotFound_WhenAccountNotCreated()
    {
        var (model, _) = CreateModel();
        // AccountQuery returns null
        var result = await model.OnPostAddAccount("Test", "test@test.com");

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnPostRemoveAccountSensor_RemovesAndRedirects()
    {
        var (model, mediator) = CreateModel();

        var result = await model.OnPostRemoveAccountSensor(Guid.NewGuid(), Guid.NewGuid());

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("Account sensor removed successfully.", redirect.RouteValues?["message"]);
        Assert.Contains(mediator.SentRequests, r => r is RemoveSensorFromAccountCommand);
    }
}
