using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CoreTests.Repositories;

public class AccountEntityTypeConfigurationTest
{
    [Fact]
    public async Task Account_TableName_IsAccount()
    {
        await using var db = TestDbContext.Create();
        var entityType = db.Context.Model.FindEntityType(typeof(Account));
        Assert.NotNull(entityType);
        Assert.Equal("Account", entityType!.GetTableName());
    }

    [Fact]
    public async Task Account_Uid_HasUniqueIndex()
    {
        await using var db = TestDbContext.Create();
        var entityType = db.Context.Model.FindEntityType(typeof(Account));
        var uidProperty = entityType!.FindProperty(nameof(Account.Uid));
        Assert.NotNull(uidProperty);

        var index = entityType.FindIndex(uidProperty!);
        Assert.NotNull(index);
        Assert.True(index!.IsUnique);
    }

    [Fact]
    public async Task Account_Email_HasUniqueIndex()
    {
        await using var db = TestDbContext.Create();
        var entityType = db.Context.Model.FindEntityType(typeof(Account));
        var emailProperty = entityType!.FindProperty(nameof(Account.Email));
        Assert.NotNull(emailProperty);

        var index = entityType.FindIndex(emailProperty!);
        Assert.NotNull(index);
        Assert.True(index!.IsUnique);
    }

    [Fact]
    public async Task Account_Link_HasUniqueIndex()
    {
        await using var db = TestDbContext.Create();
        var entityType = db.Context.Model.FindEntityType(typeof(Account));
        var linkProperty = entityType!.FindProperty(nameof(Account.Link));
        Assert.NotNull(linkProperty);

        var index = entityType.FindIndex(linkProperty!);
        Assert.NotNull(index);
        Assert.True(index!.IsUnique);
    }

    [Fact]
    public async Task Account_HasManyToManySensorsViaAccountSensor()
    {
        await using var db = TestDbContext.Create();
        var entityType = db.Context.Model.FindEntityType(typeof(Account));

        var navigation = entityType!.FindNavigation(nameof(Account.AccountSensors));
        Assert.NotNull(navigation);
    }

    [Fact]
    public async Task Account_DuplicateEmail_ThrowsException()
    {
        await using var db = TestDbContext.Create();
        db.Context.Accounts.Add(TestEntityFactory.CreateAccount("dup@test.com", "link1"));
        await db.Context.SaveChangesAsync();

        db.Context.Accounts.Add(TestEntityFactory.CreateAccount("dup@test.com", "link2"));
        await Assert.ThrowsAsync<DbUpdateException>(() => db.Context.SaveChangesAsync());
    }

    [Fact]
    public async Task Account_DuplicateUid_ThrowsException()
    {
        await using var db = TestDbContext.Create();
        var uid = Guid.NewGuid();
        db.Context.Accounts.Add(TestEntityFactory.CreateAccount("a@test.com", "linkA", uid));
        await db.Context.SaveChangesAsync();

        db.Context.Accounts.Add(TestEntityFactory.CreateAccount("b@test.com", "linkB", uid));
        await Assert.ThrowsAsync<DbUpdateException>(() => db.Context.SaveChangesAsync());
    }
}
