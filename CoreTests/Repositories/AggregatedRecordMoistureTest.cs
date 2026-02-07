using Core.Repositories;
using Xunit;

namespace CoreTests.Repositories;

public class AggregatedRecordMoistureTest
{
    [Fact]
    public void Properties_SetAndGet()
    {
        var ts = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var record = new AggregatedRecordMoisture
        {
            Timestamp = ts,
            DevEui = "A81758FFFE000003",
            BatV = 3.4,
            Rssi = -90
        };

        Assert.Equal(ts, record.Timestamp);
        Assert.Equal("A81758FFFE000003", record.DevEui);
        Assert.Equal(3.4, record.BatV);
        Assert.Equal(-90, record.Rssi);
    }
}
