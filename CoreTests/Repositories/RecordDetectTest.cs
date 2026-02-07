using Core.Repositories;
using Xunit;

namespace CoreTests.Repositories;

public class RecordDetectTest
{
    [Fact]
    public void Properties_SetAndGet()
    {
        var ts = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var record = new RecordDetect
        {
            Timestamp = ts,
            DevEui = "A81758FFFE000002",
            BatV = 3.5,
            Status = 1,
            Rssi = -85
        };

        Assert.Equal(ts, record.Timestamp);
        Assert.Equal("A81758FFFE000002", record.DevEui);
        Assert.Equal(3.5, record.BatV);
        Assert.Equal(1, record.Status);
        Assert.Equal(-85, record.Rssi);
    }

    [Fact]
    public void DefaultValues()
    {
        var record = new RecordDetect();

        Assert.Equal(default, record.Timestamp);
        Assert.Equal(default!, record.DevEui);
        Assert.Equal(0.0, record.BatV);
        Assert.Equal(0, record.Status);
        Assert.Equal(0, record.Rssi);
    }
}
