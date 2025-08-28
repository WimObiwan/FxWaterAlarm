using System;
using Core.Configuration;
using Xunit;

namespace CoreTests;

public class RemoveMeasurementCommandTest
{
    [Fact]
    public void TestMeasurementRemovalOptions_DefaultValues()
    {
        // Arrange & Act
        var config = new MeasurementRemovalOptions { TimestampToleranceSeconds = 5 };
        
        // Assert
        Assert.Equal(5, config.TimestampToleranceSeconds);
        Assert.Equal("MeasurementRemoval", MeasurementRemovalOptions.Location);
    }

    [Fact]
    public void TestMeasurementRemovalOptions_CustomValues()
    {
        // Arrange & Act
        var config = new MeasurementRemovalOptions { TimestampToleranceSeconds = 10 };
        
        // Assert
        Assert.Equal(10, config.TimestampToleranceSeconds);
    }

    [Fact]
    public void TestRemoveMeasurementCommand_Properties()
    {
        // Arrange
        var sensorUid = Guid.NewGuid();
        var timestamp = DateTime.UtcNow;
        
        // Act
        var command = new Core.Commands.RemoveMeasurementCommand
        {
            SensorUid = sensorUid,
            Timestamp = timestamp
        };
        
        // Assert
        Assert.Equal(sensorUid, command.SensorUid);
        Assert.Equal(timestamp, command.Timestamp);
    }
}