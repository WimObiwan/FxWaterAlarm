using Core.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CoreTests.Repositories;

public class WaterAlarmDbContextFactoryTest
{
    [Fact]
    public void CreateDbContext_ReturnsValidContext()
    {
        var factory = new WaterAlarmDbContextFactory();
        using var context = factory.CreateDbContext([]);

        Assert.NotNull(context);
    }

    [Fact]
    public void CreateDbContext_UsesSqlite()
    {
        var factory = new WaterAlarmDbContextFactory();
        using var context = factory.CreateDbContext([]);

        Assert.True(context.Database.IsSqlite());
    }
}
