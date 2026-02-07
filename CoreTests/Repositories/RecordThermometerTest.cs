using Core.Repositories;
using Xunit;

namespace CoreTests.Repositories;

public class RecordThermometerTest
{
    [Fact]
    public void Properties_SetAndGet()
    {
        var ts = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var record = new RecordThermometer
        {
            Timestamp = ts,
            DevEui = "A81758FFFE000004",
            BatV = 3.3,
            TempC = 22.5,
            HumPrc = 65.0,
            Rssi = -75
        };

        Assert.Equal(ts, record.Timestamp);
        Assert.Equal("A81758FFFE000004", record.DevEui);
        Assert.Equal(3.3, record.BatV);
        Assert.Equal(22.5, record.TempC);
        Assert.Equal(65.0, record.HumPrc);
        Assert.Equal(-75, record.Rssi);
    }

    [Fact]
    public void DefaultValues()
    {
        var record = new RecordThermometer();

        Assert.Equal(default, record.Timestamp);
        Assert.Equal(default!, record.DevEui);
        Assert.Equal(0.0, record.BatV);
        Assert.Equal(0.0, record.TempC);
        Assert.Equal(0.0, record.HumPrc);
        Assert.Equal(0, record.Rssi);
    }
}
