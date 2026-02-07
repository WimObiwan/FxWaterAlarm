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
