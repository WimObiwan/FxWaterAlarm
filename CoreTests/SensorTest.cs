using Core.Entities;
using Xunit;

namespace CoreTests;

public class SensorTest
{
    private static Sensor Create(SensorType type)
    {
        return new Sensor
        {
            Uid = Guid.NewGuid(),
            DevEui = "dev123",
            CreateTimestamp = DateTime.UtcNow,
            Type = type
        };
    }

    // --- SupportsCapacity ---
    [Theory]
    [InlineData(SensorType.Level, true)]
    [InlineData(SensorType.LevelPressure, true)]
    [InlineData(SensorType.Detect, false)]
    [InlineData(SensorType.Moisture, false)]
    [InlineData(SensorType.Thermometer, false)]
    public void SupportsCapacity(SensorType type, bool expected)
    {
        Assert.Equal(expected, Create(type).SupportsCapacity);
    }

    // --- SupportsDistance ---
    [Theory]
    [InlineData(SensorType.Level, true)]
    [InlineData(SensorType.LevelPressure, false)]
    [InlineData(SensorType.Detect, false)]
    [InlineData(SensorType.Moisture, false)]
    [InlineData(SensorType.Thermometer, false)]
    public void SupportsDistance(SensorType type, bool expected)
    {
        Assert.Equal(expected, Create(type).SupportsDistance);
    }

    // --- SupportsHeight ---
    [Theory]
    [InlineData(SensorType.LevelPressure, true)]
    [InlineData(SensorType.Level, false)]
    [InlineData(SensorType.Detect, false)]
    [InlineData(SensorType.Moisture, false)]
    [InlineData(SensorType.Thermometer, false)]
    public void SupportsHeight(SensorType type, bool expected)
    {
        Assert.Equal(expected, Create(type).SupportsHeight);
    }

    // --- SupportsPercentage ---
    [Theory]
    [InlineData(SensorType.Level, true)]
    [InlineData(SensorType.LevelPressure, true)]
    [InlineData(SensorType.Moisture, true)]
    [InlineData(SensorType.Detect, false)]
    [InlineData(SensorType.Thermometer, false)]
    public void SupportsPercentage(SensorType type, bool expected)
    {
        Assert.Equal(expected, Create(type).SupportsPercentage);
    }

    // --- SupportsTemperature ---
    [Theory]
    [InlineData(SensorType.Moisture, true)]
    [InlineData(SensorType.Thermometer, true)]
    [InlineData(SensorType.Level, false)]
    [InlineData(SensorType.LevelPressure, false)]
    [InlineData(SensorType.Detect, false)]
    public void SupportsTemperature(SensorType type, bool expected)
    {
        Assert.Equal(expected, Create(type).SupportsTemperature);
    }

    // --- SupportsConductivity ---
    [Theory]
    [InlineData(SensorType.Moisture, true)]
    [InlineData(SensorType.Level, false)]
    [InlineData(SensorType.LevelPressure, false)]
    [InlineData(SensorType.Detect, false)]
    [InlineData(SensorType.Thermometer, false)]
    public void SupportsConductivity(SensorType type, bool expected)
    {
        Assert.Equal(expected, Create(type).SupportsConductivity);
    }

    // --- SupportsStatus ---
    [Theory]
    [InlineData(SensorType.Detect, true)]
    [InlineData(SensorType.Level, false)]
    [InlineData(SensorType.LevelPressure, false)]
    [InlineData(SensorType.Moisture, false)]
    [InlineData(SensorType.Thermometer, false)]
    public void SupportsStatus(SensorType type, bool expected)
    {
        Assert.Equal(expected, Create(type).SupportsStatus);
    }

    // --- SupportsGraph ---
    [Theory]
    [InlineData(SensorType.Level, true)]
    [InlineData(SensorType.LevelPressure, true)]
    [InlineData(SensorType.Moisture, true)]
    [InlineData(SensorType.Detect, true)]
    [InlineData(SensorType.Thermometer, true)]
    public void SupportsGraph(SensorType type, bool expected)
    {
        Assert.Equal(expected, Create(type).SupportsGraph);
    }

    // --- SupportsMinMaxConstraints ---
    [Theory]
    [InlineData(SensorType.Level, true)]
    [InlineData(SensorType.LevelPressure, true)]
    [InlineData(SensorType.Detect, false)]
    [InlineData(SensorType.Moisture, false)]
    [InlineData(SensorType.Thermometer, false)]
    public void SupportsMinMaxConstraints(SensorType type, bool expected)
    {
        Assert.Equal(expected, Create(type).SupportsMinMaxConstraints);
    }

    // --- SupportsTrend ---
    [Theory]
    [InlineData(SensorType.Level, true)]
    [InlineData(SensorType.LevelPressure, true)]
    [InlineData(SensorType.Detect, false)]
    [InlineData(SensorType.Moisture, false)]
    [InlineData(SensorType.Thermometer, false)]
    public void SupportsTrend(SensorType type, bool expected)
    {
        Assert.Equal(expected, Create(type).SupportsTrend);
    }

    // --- SupportsDiagram ---
    [Theory]
    [InlineData(SensorType.Level, true)]
    [InlineData(SensorType.LevelPressure, true)]
    [InlineData(SensorType.Detect, false)]
    [InlineData(SensorType.Moisture, false)]
    [InlineData(SensorType.Thermometer, false)]
    public void SupportsDiagram(SensorType type, bool expected)
    {
        Assert.Equal(expected, Create(type).SupportsDiagram);
    }

    // --- SupportsAlerts ---
    [Theory]
    [InlineData(SensorType.Level, true)]
    [InlineData(SensorType.LevelPressure, true)]
    [InlineData(SensorType.Detect, true)]
    [InlineData(SensorType.Moisture, false)]
    [InlineData(SensorType.Thermometer, false)]
    public void SupportsAlerts(SensorType type, bool expected)
    {
        Assert.Equal(expected, Create(type).SupportsAlerts);
    }
}
