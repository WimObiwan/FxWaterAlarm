using Core.Queries;
using Xunit;

namespace CoreTests.Queries;

public class AccountQueryHandlerTest
{
    [Fact]
    public async Task Handle_MatchingUid_ReturnsAccount()
    {
        await using var db = TestDbContext.Create();
        var (account, _, _) = await TestEntityFactory.SeedAccountWithSensor(db.Context);
        var handler = new AccountQueryHandler(db.Context);

        var result = await handler.Handle(new AccountQuery { Uid = account.Uid }, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(account.Email, result!.Email);
    }

    [Fact]
    public async Task Handle_NoMatch_ReturnsNull()
    {
        await using var db = TestDbContext.Create();
        var handler = new AccountQueryHandler(db.Context);

        var result = await handler.Handle(new AccountQuery { Uid = Guid.NewGuid() }, CancellationToken.None);

        Assert.Null(result);
    }
}
