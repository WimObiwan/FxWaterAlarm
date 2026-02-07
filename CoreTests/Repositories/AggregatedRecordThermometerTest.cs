using Core.Repositories;
using Xunit;

namespace CoreTests.Repositories;

public class AggregatedRecordThermometerTest
{
    [Fact]
    public void Properties_SetAndGet()
    {
        var ts = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var record = new AggregatedRecordThermometer
        {
            Timestamp = ts,
            DevEui = "A81758FFFE000004",
            BatV = 3.3,
            Rssi = -75
        };

        Assert.Equal(ts, record.Timestamp);
        Assert.Equal("A81758FFFE000004", record.DevEui);
        Assert.Equal(3.3, record.BatV);
        Assert.Equal(-75, record.Rssi);
    }
}
