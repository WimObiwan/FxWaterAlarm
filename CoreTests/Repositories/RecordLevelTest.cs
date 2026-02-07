using Core.Repositories;
using Xunit;

namespace CoreTests.Repositories;

public class RecordLevelTest
{
    [Fact]
    public void Properties_SetAndGet()
    {
        var ts = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var record = new RecordLevel
        {
            Timestamp = ts,
            DevEui = "A81758FFFE000001",
            BatV = 3.6,
            Distance = 1500.0,
            Rssi = -80.0
        };

        Assert.Equal(ts, record.Timestamp);
        Assert.Equal("A81758FFFE000001", record.DevEui);
        Assert.Equal(3.6, record.BatV);
        Assert.Equal(1500.0, record.Distance);
        Assert.Equal(-80.0, record.Rssi);
    }

    [Fact]
    public void DefaultValues()
    {
        var record = new RecordLevel();

        Assert.Equal(default, record.Timestamp);
        Assert.Equal(default!, record.DevEui);
        Assert.Equal(0.0, record.BatV);
        Assert.Equal(0.0, record.Distance);
        Assert.Equal(0.0, record.Rssi);
    }
}
