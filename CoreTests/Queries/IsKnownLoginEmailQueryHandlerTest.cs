using Core.Entities;
using Core.Queries;
using Xunit;

namespace CoreTests.Queries;

public class IsKnownLoginEmailQueryHandlerTest
{
    private static async Task AddAccountUser(CoreTests.TestDbContext db, int accountId, string email)
    {
        db.Context.AccountUsers.Add(new AccountUser
        {
            AccountId = accountId,
            LoginType = AccountUserLoginType.Mail,
            Email = email,
            CreationTimestamp = DateTime.UtcNow
        });
        await db.Context.SaveChangesAsync();
    }

    [Fact]
    public async Task Handle_AccountUserEmail_ReturnsTrue()
    {
        await using var db = TestDbContext.Create();
        var (account, _, _) = await TestEntityFactory.SeedAccountWithSensor(db.Context, email: "owner@test.com");
        await AddAccountUser(db, account.Id, "user@test.com");
        var handler = new IsKnownLoginEmailQueryHandler(db.Context);

        var result = await handler.Handle(new IsKnownLoginEmailQuery { Email = "user@test.com" }, CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task Handle_AccountEmail_ReturnsTrue()
    {
        await using var db = TestDbContext.Create();
        await TestEntityFactory.SeedAccountWithSensor(db.Context, email: "owner@test.com");
        var handler = new IsKnownLoginEmailQueryHandler(db.Context);

        var result = await handler.Handle(new IsKnownLoginEmailQuery { Email = "owner@test.com" }, CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task Handle_UnknownEmail_ReturnsFalse()
    {
        await using var db = TestDbContext.Create();
        await TestEntityFactory.SeedAccountWithSensor(db.Context, email: "owner@test.com");
        var handler = new IsKnownLoginEmailQueryHandler(db.Context);

        var result = await handler.Handle(new IsKnownLoginEmailQuery { Email = "attacker@evil.com" }, CancellationToken.None);

        Assert.False(result);
    }

    [Theory]
    [InlineData("OWNER@TEST.COM")]
    [InlineData("  owner@test.com  ")]
    public async Task Handle_IgnoresCaseAndSurroundingWhitespace(string email)
    {
        await using var db = TestDbContext.Create();
        await TestEntityFactory.SeedAccountWithSensor(db.Context, email: "owner@test.com");
        var handler = new IsKnownLoginEmailQueryHandler(db.Context);

        var result = await handler.Handle(new IsKnownLoginEmailQuery { Email = email }, CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task Handle_GoogleLoginTypeOnly_ReturnsFalse()
    {
        await using var db = TestDbContext.Create();
        var (account, _, _) = await TestEntityFactory.SeedAccountWithSensor(db.Context, email: "owner@test.com");
        db.Context.AccountUsers.Add(new AccountUser
        {
            AccountId = account.Id,
            LoginType = AccountUserLoginType.Google,
            Email = "google-only@test.com",
            Provider = "google",
            ProviderSubjectId = "sub-1",
            CreationTimestamp = DateTime.UtcNow
        });
        await db.Context.SaveChangesAsync();
        var handler = new IsKnownLoginEmailQueryHandler(db.Context);

        var result = await handler.Handle(new IsKnownLoginEmailQuery { Email = "google-only@test.com" }, CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task Handle_EmptyEmail_ReturnsFalse()
    {
        await using var db = TestDbContext.Create();
        await TestEntityFactory.SeedAccountWithSensor(db.Context, email: "owner@test.com");
        var handler = new IsKnownLoginEmailQueryHandler(db.Context);

        var result = await handler.Handle(new IsKnownLoginEmailQuery { Email = "   " }, CancellationToken.None);

        Assert.False(result);
    }
}
