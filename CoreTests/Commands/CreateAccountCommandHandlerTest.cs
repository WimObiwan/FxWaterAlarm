using Core.Commands;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CoreTests.Commands;

public class CreateAccountCommandHandlerTest
{
    [Fact]
    public async Task Handle_CreatesAccountInDatabase()
    {
        await using var db = TestDbContext.Create();
        var handler = new CreateAccountCommandHandler(db.Context);
        var uid = Guid.NewGuid();

        await handler.Handle(new CreateAccountCommand
        {
            Uid = uid,
            Email = "new@test.com",
            Name = "Test User"
        }, CancellationToken.None);

        var account = await db.Context.Accounts.SingleOrDefaultAsync(a => a.Uid == uid);
        Assert.NotNull(account);
        Assert.Equal("new@test.com", account!.Email);
        Assert.Equal("Test User", account.Name);
    }

    [Fact]
    public async Task Handle_WithoutName_CreatesAccountWithNullName()
    {
        await using var db = TestDbContext.Create();
        var handler = new CreateAccountCommandHandler(db.Context);
        var uid = Guid.NewGuid();

        await handler.Handle(new CreateAccountCommand
        {
            Uid = uid,
            Email = "noname@test.com"
        }, CancellationToken.None);

        var account = await db.Context.Accounts.SingleOrDefaultAsync(a => a.Uid == uid);
        Assert.NotNull(account);
        Assert.Null(account!.Name);
    }

    [Fact]
    public async Task Handle_SetsCreationTimestamp()
    {
        await using var db = TestDbContext.Create();
        var handler = new CreateAccountCommandHandler(db.Context);
        var uid = Guid.NewGuid();
        var before = DateTime.UtcNow;

        await handler.Handle(new CreateAccountCommand
        {
            Uid = uid,
            Email = "ts@test.com"
        }, CancellationToken.None);

        var account = await db.Context.Accounts.SingleAsync(a => a.Uid == uid);
        Assert.True(account.CreationTimestamp >= before);
        Assert.True(account.CreationTimestamp <= DateTime.UtcNow);
    }

    [Fact]
    public async Task Handle_DuplicateEmail_ThrowsDbUpdateException()
    {
        await using var db = TestDbContext.Create();
        var handler = new CreateAccountCommandHandler(db.Context);

        await handler.Handle(new CreateAccountCommand
        {
            Uid = Guid.NewGuid(),
            Email = "dup@test.com"
        }, CancellationToken.None);

        await Assert.ThrowsAsync<DbUpdateException>(() =>
            handler.Handle(new CreateAccountCommand
            {
                Uid = Guid.NewGuid(),
                Email = "dup@test.com"
            }, CancellationToken.None));
    }
}
