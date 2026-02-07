using Core.Repositories;
using Xunit;

namespace CoreTests.Repositories;

public class AggregatedRecordDetectTest
{
    [Fact]
    public void Properties_SetAndGet()
    {
        var ts = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var record = new AggregatedRecordDetect
        {
            Timestamp = ts,
            DevEui = "A81758FFFE000002",
            BatV = 3.5,
            Rssi = -85
        };

        Assert.Equal(ts, record.Timestamp);
        Assert.Equal("A81758FFFE000002", record.DevEui);
        Assert.Equal(3.5, record.BatV);
        Assert.Equal(-85, record.Rssi);
    }
}
