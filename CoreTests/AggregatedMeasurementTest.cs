using Core.Entities;
using Xunit;

namespace CoreTests;

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
