using Core.Entities;
using Core.Queries;
using Xunit;

namespace CoreTests.Queries;

public class LastMedianMeasurementQueryHandlerTest
{
    [Fact]
    public async Task Handle_ReturnsAggregatedMeasurement()
    {
        var levelRepo = new FakeMeasurementLevelRepository
        {
            LastMedianResult = new AggregatedMeasurement
            {
                DevEui = "dev123", BatV = 3.5, RssiDbm = -80
            }
        };
        var handler = new LastMedianMeasurementQueryHandler(levelRepo);

        var result = await handler.Handle(
            new LastMedianMeasurementQuery { DevEui = "dev123", From = DateTime.UtcNow.AddHours(-1) },
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("dev123", result!.DevEui);
    }

    [Fact]
    public async Task Handle_NullResult_ReturnsNull()
    {
        var levelRepo = new FakeMeasurementLevelRepository { LastMedianResult = null };
        var handler = new LastMedianMeasurementQueryHandler(levelRepo);

        var result = await handler.Handle(
            new LastMedianMeasurementQuery { DevEui = "dev123", From = DateTime.UtcNow },
            CancellationToken.None);

        Assert.Null(result);
    }
}
