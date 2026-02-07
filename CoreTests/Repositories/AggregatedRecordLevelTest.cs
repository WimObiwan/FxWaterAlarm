using Core.Repositories;
using Xunit;

namespace CoreTests.Repositories;

public class AggregatedRecordLevelTest
{
    [Fact]
    public void Properties_SetAndGet()
    {
        var ts = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var record = new AggregatedRecordLevel
        {
            Timestamp = ts,
            DevEui = "A81758FFFE000001",
            BatV = 3.6,
            MinDistance = 1200.0,
            MeanDistance = 1400.0,
            MaxDistance = 1800.0,
            LastDistance = 1500.0,
            Rssi = -80.0
        };

        Assert.Equal(ts, record.Timestamp);
        Assert.Equal("A81758FFFE000001", record.DevEui);
        Assert.Equal(3.6, record.BatV);
        Assert.Equal(1200.0, record.MinDistance);
        Assert.Equal(1400.0, record.MeanDistance);
        Assert.Equal(1800.0, record.MaxDistance);
        Assert.Equal(1500.0, record.LastDistance);
        Assert.Equal(-80.0, record.Rssi);
    }

    [Fact]
    public void NullableFields_DefaultToNull()
    {
        var record = new AggregatedRecordLevel();

        Assert.Null(record.MinDistance);
        Assert.Null(record.MeanDistance);
        Assert.Null(record.MaxDistance);
        Assert.Null(record.LastDistance);
    }
}
