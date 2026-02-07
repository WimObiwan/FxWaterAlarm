using Core.Entities;
using Xunit;

namespace CoreTests;

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
