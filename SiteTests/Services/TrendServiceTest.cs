using Core.Entities;
using Core.Queries;
using Core.Util;
using MediatR;
using Site.Services;
using Xunit;

namespace SiteTests.Services;

public class TrendServiceTest
{
    private class FakeMediator : IMediator
    {
        public IMeasurementEx? ResponseMeasurement { get; set; }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (request is LastMeasurementBeforeQuery)
            {
                return Task.FromResult((TResponse)ResponseMeasurement!);
            }
            throw new NotSupportedException($"Unexpected request type: {request.GetType()}");
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
            => Task.CompletedTask;
        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
            => Task.FromResult<object?>(null);
        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
            => AsyncEnumerable.Empty<TResponse>();
        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
            => AsyncEnumerable.Empty<object?>();
        public Task Publish(object notification, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification
            => Task.CompletedTask;
    }

    private static Core.Entities.AccountSensor CreateAccountSensor()
    {
        return new Core.Entities.AccountSensor
        {
            Account = new Core.Entities.Account
            {
                Uid = Guid.NewGuid(),
                Email = "test@example.com",
                CreationTimestamp = DateTime.UtcNow,
            },
            Sensor = new Core.Entities.Sensor
            {
                Uid = Guid.NewGuid(),
                DevEui = "test-sensor",
                CreateTimestamp = DateTime.UtcNow,
                Type = SensorType.Level,
                ExpectedIntervalSecs = 3600
            },
            CreateTimestamp = DateTime.UtcNow,
            DistanceMmEmpty = 2000,
            DistanceMmFull = 500,
            CapacityL = 5000
        };
    }

    private static MeasurementLevelEx CreateMeasurementLevelEx(
        Core.Entities.AccountSensor accountSensor,
        DateTime timestamp,
        int distanceMm = 1000,
        double batV = 3.3,
        double rssiDbm = -90)
    {
        return new MeasurementLevelEx(
            new MeasurementLevel
            {
                DevEui = accountSensor.Sensor.DevEui,
                Timestamp = timestamp,
                BatV = batV,
                RssiDbm = rssiDbm,
                DistanceMm = distanceMm
            },
            accountSensor);
    }

    [Fact]
    public async Task GetTrendMeasurement_ReturnsNull_WhenNoOlderMeasurement()
    {
        var mediator = new FakeMediator { ResponseMeasurement = null };
        var service = new TrendService(mediator);
        var accountSensor = CreateAccountSensor();
        var current = CreateMeasurementLevelEx(accountSensor, DateTime.UtcNow);

        var result = await service.GetTrendMeasurement(TimeSpan.FromHours(6), current);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetTrendMeasurement_ReturnsTrend_WhenOlderMeasurementExists()
    {
        var accountSensor = CreateAccountSensor();
        var now = DateTime.UtcNow;
        var older = CreateMeasurementLevelEx(accountSensor, now.AddHours(-6), distanceMm: 1200);
        var current = CreateMeasurementLevelEx(accountSensor, now, distanceMm: 1000);

        var mediator = new FakeMediator { ResponseMeasurement = older };
        var service = new TrendService(mediator);

        var result = await service.GetTrendMeasurement(TimeSpan.FromHours(6), current);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task GetTrendMeasurements_ReturnsArrayOfCorrectLength()
    {
        var accountSensor = CreateAccountSensor();
        var now = DateTime.UtcNow;
        var older = CreateMeasurementLevelEx(accountSensor, now.AddHours(-6), distanceMm: 1200);
        var current = CreateMeasurementLevelEx(accountSensor, now, distanceMm: 1000);

        var mediator = new FakeMediator { ResponseMeasurement = older };
        var service = new TrendService(mediator);

        var result = await service.GetTrendMeasurements(current,
            TimeSpan.FromHours(6), TimeSpan.FromHours(24));

        Assert.Equal(2, result.Length);
    }

    [Fact]
    public async Task GetTrendMeasurements_ReturnsNullEntries_WhenNoOlderMeasurements()
    {
        var accountSensor = CreateAccountSensor();
        var current = CreateMeasurementLevelEx(accountSensor, DateTime.UtcNow);

        var mediator = new FakeMediator { ResponseMeasurement = null };
        var service = new TrendService(mediator);

        var result = await service.GetTrendMeasurements(current,
            TimeSpan.FromHours(6), TimeSpan.FromHours(24), TimeSpan.FromDays(7));

        Assert.Equal(3, result.Length);
        Assert.All(result, r => Assert.Null(r));
    }

    [Fact]
    public async Task GetTrendMeasurements_WithFourTimeSpans()
    {
        var accountSensor = CreateAccountSensor();
        var now = DateTime.UtcNow;
        var older = CreateMeasurementLevelEx(accountSensor, now.AddHours(-1), distanceMm: 1100);
        var current = CreateMeasurementLevelEx(accountSensor, now, distanceMm: 1000);

        var mediator = new FakeMediator { ResponseMeasurement = older };
        var service = new TrendService(mediator);

        var result = await service.GetTrendMeasurements(current,
            TimeSpan.FromHours(6), TimeSpan.FromHours(24),
            TimeSpan.FromDays(7), TimeSpan.FromDays(30));

        Assert.Equal(4, result.Length);
        Assert.All(result, r => Assert.NotNull(r));
    }
}
