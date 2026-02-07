using Core.Commands;
using Core.Exceptions;
using Core.Util;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CoreTests.Commands;

public class UpdateAccountCommandHandlerTest
{
    [Fact]
    public async Task Handle_UpdatesEmail()
    {
        await using var db = TestDbContext.Create();
        var account = TestEntityFactory.CreateAccount("old@test.com", "uatest");
        db.Context.Accounts.Add(account);
        await db.Context.SaveChangesAsync();

        var handler = new UpdateAccountCommandHandler(db.Context);
        await handler.Handle(new UpdateAccountCommand
        {
            Uid = account.Uid,
            Email = new Optional<string>(true, "new@test.com")
        }, CancellationToken.None);

        var updated = await db.Context.Accounts.SingleAsync(a => a.Uid == account.Uid);
        Assert.Equal("new@test.com", updated.Email);
    }

    [Fact]
    public async Task Handle_UpdatesName()
    {
        await using var db = TestDbContext.Create();
        var account = TestEntityFactory.CreateAccount("name@test.com", "ualink");
        db.Context.Accounts.Add(account);
        await db.Context.SaveChangesAsync();

        var handler = new UpdateAccountCommandHandler(db.Context);
        await handler.Handle(new UpdateAccountCommand
        {
            Uid = account.Uid,
            Name = new Optional<string>(true, "New Name")
        }, CancellationToken.None);

        var updated = await db.Context.Accounts.SingleAsync(a => a.Uid == account.Uid);
        Assert.Equal("New Name", updated.Name);
    }

    [Fact]
    public async Task Handle_UnspecifiedFieldsUnchanged()
    {
        await using var db = TestDbContext.Create();
        var account = TestEntityFactory.CreateAccount("keep@test.com", "keeplink");
        account.Name = "Keep Me";
        db.Context.Accounts.Add(account);
        await db.Context.SaveChangesAsync();

        var handler = new UpdateAccountCommandHandler(db.Context);
        await handler.Handle(new UpdateAccountCommand
        {
            Uid = account.Uid,
            // Email and Name not specified (default Optional<T> has Specified=false)
        }, CancellationToken.None);

        var updated = await db.Context.Accounts.SingleAsync(a => a.Uid == account.Uid);
        Assert.Equal("keep@test.com", updated.Email);
        Assert.Equal("Keep Me", updated.Name);
    }

    [Fact]
    public async Task Handle_SetNameToNull()
    {
        await using var db = TestDbContext.Create();
        var account = TestEntityFactory.CreateAccount("nullname@test.com", "nnlink");
        account.Name = "Was Something";
        db.Context.Accounts.Add(account);
        await db.Context.SaveChangesAsync();

        var handler = new UpdateAccountCommandHandler(db.Context);
        await handler.Handle(new UpdateAccountCommand
        {
            Uid = account.Uid,
            Name = new Optional<string>(true, null)
        }, CancellationToken.None);

        var updated = await db.Context.Accounts.SingleAsync(a => a.Uid == account.Uid);
        Assert.Null(updated.Name);
    }

    [Fact]
    public async Task Handle_SetEmailToNull_ThrowsArgumentNullException()
    {
        await using var db = TestDbContext.Create();
        var account = TestEntityFactory.CreateAccount("nonull@test.com", "nonulllink");
        db.Context.Accounts.Add(account);
        await db.Context.SaveChangesAsync();

        var handler = new UpdateAccountCommandHandler(db.Context);

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            handler.Handle(new UpdateAccountCommand
            {
                Uid = account.Uid,
                Email = new Optional<string>(true, null)
            }, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_AccountNotFound_ThrowsAccountNotFoundException()
    {
        await using var db = TestDbContext.Create();
        var handler = new UpdateAccountCommandHandler(db.Context);

        await Assert.ThrowsAsync<AccountNotFoundException>(() =>
            handler.Handle(new UpdateAccountCommand
            {
                Uid = Guid.NewGuid(),
                Email = new Optional<string>(true, "x@test.com")
            }, CancellationToken.None));
    }
}
