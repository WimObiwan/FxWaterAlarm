using Core.Commands;
using Core.Exceptions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CoreTests.Commands;

public class RegenerateAccountLinkCommandHandlerTest
{
    [Fact]
    public async Task Handle_WithExplicitLink_SetsLink()
    {
        await using var db = TestDbContext.Create();
        var account = TestEntityFactory.CreateAccount("regen@test.com", "oldlink");
        db.Context.Accounts.Add(account);
        await db.Context.SaveChangesAsync();

        var handler = new RegenerateAccountLinkCommandHandler(db.Context);
        await handler.Handle(new RegenerateAccountLinkCommand
        {
            AccountUid = account.Uid,
            Link = "newlink123"
        }, CancellationToken.None);

        var updated = await db.Context.Accounts.SingleAsync(a => a.Uid == account.Uid);
        Assert.Equal("newlink123", updated.Link);
    }

    [Fact]
    public async Task Handle_WithoutLink_GeneratesRandomLink()
    {
        await using var db = TestDbContext.Create();
        var account = TestEntityFactory.CreateAccount("regenrnd@test.com", "origlink");
        db.Context.Accounts.Add(account);
        await db.Context.SaveChangesAsync();

        var handler = new RegenerateAccountLinkCommandHandler(db.Context);
        await handler.Handle(new RegenerateAccountLinkCommand
        {
            AccountUid = account.Uid
        }, CancellationToken.None);

        var updated = await db.Context.Accounts.SingleAsync(a => a.Uid == account.Uid);
        Assert.NotNull(updated.Link);
        Assert.NotEqual("origlink", updated.Link);
    }

    [Fact]
    public async Task Handle_AccountNotFound_ThrowsAccountNotFoundException()
    {
        await using var db = TestDbContext.Create();
        var handler = new RegenerateAccountLinkCommandHandler(db.Context);

        await Assert.ThrowsAsync<AccountNotFoundException>(() =>
            handler.Handle(new RegenerateAccountLinkCommand
            {
                AccountUid = Guid.NewGuid(),
                Link = "irrelevant"
            }, CancellationToken.None));
    }
}
