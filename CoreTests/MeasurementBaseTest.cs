using Core.Entities;
using Xunit;

namespace CoreTests;

public class MeasurementBaseTest
{
    [Fact]
    public void GetValues_ReturnsEmptyDictionary()
    {
        var measurement = new Measurement
        {
            DevEui = "dev123",
            Timestamp = DateTime.UtcNow,
            BatV = 3.6,
            RssiDbm = -80
        };
        var values = measurement.GetValues();
        Assert.NotNull(values);
        Assert.Empty(values);
    }

    [Fact]
    public void Properties_AreSetCorrectly()
    {
        var ts = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        var measurement = new Measurement
        {
            DevEui = "dev456",
            Timestamp = ts,
            BatV = 3.3,
            RssiDbm = -95.5
        };
        Assert.Equal("dev456", measurement.DevEui);
        Assert.Equal(ts, measurement.Timestamp);
        Assert.Equal(3.3, measurement.BatV);
        Assert.Equal(-95.5, measurement.RssiDbm);
    }
}

public class AggregatedMeasurementTest
{
    [Fact]
    public void Properties_AreSetCorrectly()
    {
        var ts = new DateTime(2024, 1, 15, 10, 30, 0, DateTimeKind.Utc);
        var agg = new AggregatedMeasurement
        {
            DevEui = "dev789",
            Timestamp = ts,
            BatV = 3.2,
            RssiDbm = -100
        };
        Assert.Equal("dev789", agg.DevEui);
        Assert.Equal(ts, agg.Timestamp);
        Assert.Equal(3.2, agg.BatV);
        Assert.Equal(-100, agg.RssiDbm);
    }
}

public class AggregatedMeasurementLevelTest
{
    [Fact]
    public void Properties_AreSetCorrectly()
    {
        var agg = new AggregatedMeasurementLevel
        {
            DevEui = "dev789",
            Timestamp = DateTime.UtcNow,
            BatV = 3.5,
            RssiDbm = -85,
            LastDistanceMm = 1500,
            MinDistanceMm = 1200,
            MeanDistanceMm = 1400,
            MaxDistanceMm = 1800
        };
        Assert.Equal(1500, agg.LastDistanceMm);
        Assert.Equal(1200, agg.MinDistanceMm);
        Assert.Equal(1400, agg.MeanDistanceMm);
        Assert.Equal(1800, agg.MaxDistanceMm);
    }

    [Fact]
    public void NullProperties_DefaultToNull()
    {
        var agg = new AggregatedMeasurementLevel
        {
            DevEui = "dev789",
            Timestamp = DateTime.UtcNow,
            BatV = 3.5,
            RssiDbm = -85
        };
        Assert.Null(agg.LastDistanceMm);
        Assert.Null(agg.MinDistanceMm);
        Assert.Null(agg.MeanDistanceMm);
        Assert.Null(agg.MaxDistanceMm);
    }

    [Fact]
    public void InheritsFromAggregatedMeasurement()
    {
        var agg = new AggregatedMeasurementLevel
        {
            DevEui = "d",
            BatV = 1,
            RssiDbm = -1
        };
        Assert.IsAssignableFrom<AggregatedMeasurement>(agg);
    }
}
