using Core.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CoreTests.Repositories;

public class SensorEntityTypeConfigurationTest
{
    [Fact]
    public async Task Sensor_TableName_IsSensor()
    {
        await using var db = TestDbContext.Create();
        var entityType = db.Context.Model.FindEntityType(typeof(Sensor));
        Assert.NotNull(entityType);
        Assert.Equal("Sensor", entityType!.GetTableName());
    }

    [Fact]
    public async Task Sensor_Uid_HasUniqueIndex()
    {
        await using var db = TestDbContext.Create();
        var entityType = db.Context.Model.FindEntityType(typeof(Sensor));
        var uidProperty = entityType!.FindProperty(nameof(Sensor.Uid));
        Assert.NotNull(uidProperty);

        var index = entityType.FindIndex(uidProperty!);
        Assert.NotNull(index);
        Assert.True(index!.IsUnique);
    }

    [Fact]
    public async Task Sensor_Link_HasUniqueIndex()
    {
        await using var db = TestDbContext.Create();
        var entityType = db.Context.Model.FindEntityType(typeof(Sensor));
        var linkProperty = entityType!.FindProperty(nameof(Sensor.Link));
        Assert.NotNull(linkProperty);

        var index = entityType.FindIndex(linkProperty!);
        Assert.NotNull(index);
        Assert.True(index!.IsUnique);
    }

    [Fact]
    public async Task Sensor_Type_DefaultsToLevel()
    {
        await using var db = TestDbContext.Create();
        var entityType = db.Context.Model.FindEntityType(typeof(Sensor));
        var typeProperty = entityType!.FindProperty(nameof(Sensor.Type));
        Assert.NotNull(typeProperty);

        var defaultValue = typeProperty!.GetDefaultValue();
        Assert.Equal(SensorType.Level, defaultValue);
    }

    [Fact]
    public async Task Sensor_DuplicateLink_ThrowsException()
    {
        await using var db = TestDbContext.Create();
        db.Context.Sensors.Add(TestEntityFactory.CreateSensor(link: "duplink"));
        await db.Context.SaveChangesAsync();

        db.Context.Sensors.Add(TestEntityFactory.CreateSensor(link: "duplink"));
        await Assert.ThrowsAsync<DbUpdateException>(() => db.Context.SaveChangesAsync());
    }

    [Fact]
    public async Task Sensor_DuplicateUid_ThrowsException()
    {
        await using var db = TestDbContext.Create();
        var uid = Guid.NewGuid();
        db.Context.Sensors.Add(TestEntityFactory.CreateSensor(link: "s1", uid: uid));
        await db.Context.SaveChangesAsync();

        db.Context.Sensors.Add(TestEntityFactory.CreateSensor(link: "s2", uid: uid));
        await Assert.ThrowsAsync<DbUpdateException>(() => db.Context.SaveChangesAsync());
    }
}
