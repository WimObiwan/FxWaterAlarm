using Core.Repositories;
using Xunit;

namespace CoreTests.Repositories;

public class MeasurementInfluxOptionsTest
{
    [Fact]
    public void Position_ReturnsExpectedValue()
    {
        Assert.Equal("Influx", MeasurementInfluxOptions.Position);
    }

    [Fact]
    public void Properties_SetAndGet()
    {
        var options = new MeasurementInfluxOptions
        {
            Endpoint = new Uri("http://localhost:8086"),
            Username = "admin",
            Password = "secret"
        };

        Assert.Equal(new Uri("http://localhost:8086"), options.Endpoint);
        Assert.Equal("admin", options.Username);
        Assert.Equal("secret", options.Password);
    }

    [Fact]
    public void DefaultValues_AreDefault()
    {
        var options = new MeasurementInfluxOptions();

        Assert.Equal(default, options.Endpoint);
        Assert.Equal(default, options.Username);
        Assert.Equal(default, options.Password);
    }
}
