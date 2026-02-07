using Core.Queries;
using Xunit;

namespace CoreTests.Queries;

public class AccountsQueryHandlerTest
{
    [Fact]
    public async Task Handle_ReturnsAllAccounts()
    {
        await using var db = TestDbContext.Create();
        var ctx = db.Context;
        ctx.Accounts.Add(TestEntityFactory.CreateAccount("a1@t.com", "l1"));
        ctx.Accounts.Add(TestEntityFactory.CreateAccount("a2@t.com", "l2"));
        await ctx.SaveChangesAsync();
        var handler = new AccountsQueryHandler(ctx);

        var result = await handler.Handle(new AccountsQuery(), CancellationToken.None);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task Handle_WithIncludeAccountSensors_IncludesSensors()
    {
        await using var db = TestDbContext.Create();
        await TestEntityFactory.SeedAccountWithSensor(db.Context, email: "ias@t.com", accountLink: "iasl");
        var handler = new AccountsQueryHandler(db.Context);

        var result = await handler.Handle(new AccountsQuery { IncludeAccountSensors = true }, CancellationToken.None);

        Assert.Single(result);
        Assert.Single(result[0].AccountSensors);
    }

    [Fact]
    public async Task Handle_Empty_ReturnsEmptyList()
    {
        await using var db = TestDbContext.Create();
        var handler = new AccountsQueryHandler(db.Context);

        var result = await handler.Handle(new AccountsQuery(), CancellationToken.None);

        Assert.Empty(result);
    }
}
