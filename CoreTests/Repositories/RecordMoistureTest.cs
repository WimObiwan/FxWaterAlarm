using Core.Repositories;
using Xunit;

namespace CoreTests.Repositories;

public class RecordMoistureTest
{
    [Fact]
    public void Properties_SetAndGet()
    {
        var ts = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var record = new RecordMoisture
        {
            Timestamp = ts,
            DevEui = "A81758FFFE000003",
            BatV = 3.4,
            SoilConductivity = 150,
            SoilMoisturePrc = 65.5,
            SoilTemperature = 18.2,
            Rssi = -90
        };

        Assert.Equal(ts, record.Timestamp);
        Assert.Equal("A81758FFFE000003", record.DevEui);
        Assert.Equal(3.4, record.BatV);
        Assert.Equal(150, record.SoilConductivity);
        Assert.Equal(65.5, record.SoilMoisturePrc);
        Assert.Equal(18.2, record.SoilTemperature);
        Assert.Equal(-90, record.Rssi);
    }

    [Fact]
    public void DefaultValues()
    {
        var record = new RecordMoisture();

        Assert.Equal(default, record.Timestamp);
        Assert.Equal(default!, record.DevEui);
        Assert.Equal(0.0, record.BatV);
        Assert.Equal(0, record.SoilConductivity);
        Assert.Equal(0.0, record.SoilMoisturePrc);
        Assert.Equal(0.0, record.SoilTemperature);
        Assert.Equal(0, record.Rssi);
    }
}
