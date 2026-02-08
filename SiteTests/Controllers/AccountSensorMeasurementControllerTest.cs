using Core.Commands;
using Core.Entities;
using Core.Queries;
using Core.Util;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Site.Controllers;
using Site.Utilities;
using SiteTests.Helpers;

namespace SiteTests.Controllers;

public class AccountSensorMeasurementControllerTest
{
    private class MeasurementFakeMediator : IMediator
    {
        public readonly List<object> SentRequests = new();
        public Core.Entities.AccountSensor? AccountSensor { get; set; }
        public IMeasurementEx[]? Measurements { get; set; }
        public bool ThrowInvalidOperation { get; set; }
        public FakeUserInfo? UserInfo { get; set; }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            SentRequests.Add(request);
            if (request is AccountSensorByLinkQuery)
                return Task.FromResult((TResponse)(object)AccountSensor!);
            if (request is MeasurementsQuery)
                return Task.FromResult((TResponse)(object)Measurements!);
            return Task.FromResult(default(TResponse)!);
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
        {
            SentRequests.Add(request!);
            if (ThrowInvalidOperation) throw new InvalidOperationException("Test error");
            return Task.CompletedTask;
        }

        public Task<object?> Send(object request, CancellationToken cancellationToken = default) => Task.FromResult<object?>(null);
        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) => AsyncEnumerable.Empty<TResponse>();
        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) => AsyncEnumerable.Empty<object?>();
        public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification => Task.CompletedTask;
    }

    private static (AccountSensorMeasurementController controller, MeasurementFakeMediator mediator) CreateController(FakeUserInfo? userInfo = null)
    {
        var mediator = new MeasurementFakeMediator();
        mediator.UserInfo = userInfo;
        var controller = new AccountSensorMeasurementController(mediator);
        var httpContext = new DefaultHttpContext();
        if (userInfo != null)
        {
            var serviceCollection = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
            serviceCollection.AddSingleton<IUserInfo>(userInfo);
            httpContext.RequestServices = serviceCollection.BuildServiceProvider();
        }
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
        return (controller, mediator);
    }

    [Fact]
    public async Task Index_ReturnsNotFound_WhenAccountSensorNotFound()
    {
        var (controller, _) = CreateController();

        var result = await controller.Index("a", "s");

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Index_ReturnsNoContent_WhenGraphTypeIsNone()
    {
        var (controller, mediator) = CreateController();
        var sensor = TestEntityFactory.CreateSensor(type: SensorType.Detect);
        var accountSensor = TestEntityFactory.CreateAccountSensor(
            sensor: sensor, distanceMmEmpty: null, distanceMmFull: null, capacityL: null);
        mediator.AccountSensor = accountSensor;

        var result = await controller.Index("a", "s", graphType: GraphType.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task Index_ReturnsOk_WithHeightData()
    {
        var (controller, mediator) = CreateController();
        var accountSensor = TestEntityFactory.CreateAccountSensor();
        mediator.AccountSensor = accountSensor;
        var m = TestEntityFactory.CreateMeasurementLevelEx(accountSensor, distanceMm: 1000);
        mediator.Measurements = new IMeasurementEx[] { m };

        var result = await controller.Index("a", "s", graphType: GraphType.Height);

        var ok = Assert.IsType<OkObjectResult>(result);
        var measurementResult = Assert.IsType<MeasurementResult>(ok.Value);
        Assert.Equal("mm", measurementResult.Unit);
        Assert.NotNull(measurementResult.Data);
    }

    [Fact]
    public async Task Index_ReturnsOk_WithPercentageData()
    {
        var (controller, mediator) = CreateController();
        var accountSensor = TestEntityFactory.CreateAccountSensor();
        mediator.AccountSensor = accountSensor;
        var m = TestEntityFactory.CreateMeasurementLevelEx(accountSensor, distanceMm: 1000);
        mediator.Measurements = new IMeasurementEx[] { m };

        var result = await controller.Index("a", "s", graphType: GraphType.Percentage);

        var ok = Assert.IsType<OkObjectResult>(result);
        var measurementResult = Assert.IsType<MeasurementResult>(ok.Value);
        Assert.Equal("%", measurementResult.Unit);
    }

    [Fact]
    public async Task Index_ReturnsOk_WithVolumeData()
    {
        var (controller, mediator) = CreateController();
        var accountSensor = TestEntityFactory.CreateAccountSensor();
        mediator.AccountSensor = accountSensor;
        var m = TestEntityFactory.CreateMeasurementLevelEx(accountSensor, distanceMm: 1000);
        mediator.Measurements = new IMeasurementEx[] { m };

        var result = await controller.Index("a", "s", graphType: GraphType.Volume);

        var ok = Assert.IsType<OkObjectResult>(result);
        var measurementResult = Assert.IsType<MeasurementResult>(ok.Value);
        Assert.Equal("l", measurementResult.Unit);
    }

    [Fact]
    public async Task Index_ReturnsOk_WithDistanceData()
    {
        var (controller, mediator) = CreateController();
        var accountSensor = TestEntityFactory.CreateAccountSensor();
        mediator.AccountSensor = accountSensor;
        var m = TestEntityFactory.CreateMeasurementLevelEx(accountSensor, distanceMm: 1000);
        mediator.Measurements = new IMeasurementEx[] { m };

        var result = await controller.Index("a", "s", graphType: GraphType.Distance);

        var ok = Assert.IsType<OkObjectResult>(result);
        var measurementResult = Assert.IsType<MeasurementResult>(ok.Value);
        Assert.Equal("mm", measurementResult.Unit);
    }

    [Fact]
    public async Task Index_ReturnsOk_WithStatusData()
    {
        var (controller, mediator) = CreateController();
        var sensor = TestEntityFactory.CreateSensor(type: SensorType.Detect);
        var accountSensor = TestEntityFactory.CreateAccountSensor(sensor: sensor);
        mediator.AccountSensor = accountSensor;
        mediator.Measurements = new IMeasurementEx[]
        {
            TestEntityFactory.CreateMeasurementDetectEx(accountSensor, status: 1)
        };

        var result = await controller.Index("a", "s", graphType: GraphType.Status);

        var ok = Assert.IsType<OkObjectResult>(result);
        var measurementResult = Assert.IsType<MeasurementResult>(ok.Value);
        Assert.Equal("", measurementResult.Unit);
    }

    [Fact]
    public async Task Index_ReturnsOk_WithRssiData()
    {
        var (controller, mediator) = CreateController();
        var accountSensor = TestEntityFactory.CreateAccountSensor();
        mediator.AccountSensor = accountSensor;
        mediator.Measurements = new IMeasurementEx[]
        {
            TestEntityFactory.CreateMeasurementLevelEx(accountSensor)
        };

        var result = await controller.Index("a", "s", graphType: GraphType.RssiDbm);

        var ok = Assert.IsType<OkObjectResult>(result);
        var measurementResult = Assert.IsType<MeasurementResult>(ok.Value);
        Assert.Equal("dBm", measurementResult.Unit);
    }

    [Fact]
    public async Task Index_ReturnsOk_WithBatVData()
    {
        var (controller, mediator) = CreateController();
        var accountSensor = TestEntityFactory.CreateAccountSensor();
        mediator.AccountSensor = accountSensor;
        mediator.Measurements = new IMeasurementEx[]
        {
            TestEntityFactory.CreateMeasurementLevelEx(accountSensor)
        };

        var result = await controller.Index("a", "s", graphType: GraphType.BatV);

        var ok = Assert.IsType<OkObjectResult>(result);
        var measurementResult = Assert.IsType<MeasurementResult>(ok.Value);
        Assert.Equal("V", measurementResult.Unit);
    }

    [Fact]
    public async Task Index_ReturnsOk_WithEmptyData()
    {
        var (controller, mediator) = CreateController();
        var accountSensor = TestEntityFactory.CreateAccountSensor();
        mediator.AccountSensor = accountSensor;
        mediator.Measurements = Array.Empty<IMeasurementEx>();

        var result = await controller.Index("a", "s", graphType: GraphType.Height);

        var ok = Assert.IsType<OkObjectResult>(result);
        var measurementResult = Assert.IsType<MeasurementResult>(ok.Value);
        Assert.Equal("mm", measurementResult.Unit);
    }

    [Fact]
    public async Task Index_CapsFromDaysAt365()
    {
        var (controller, mediator) = CreateController();
        var accountSensor = TestEntityFactory.CreateAccountSensor();
        mediator.AccountSensor = accountSensor;
        mediator.Measurements = Array.Empty<IMeasurementEx>();

        var result = await controller.Index("a", "s", fromDays: 999, graphType: GraphType.Height);

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Index_ReturnsOk_WithTemperatureData_FromMoisture()
    {
        var (controller, mediator) = CreateController();
        var sensor = TestEntityFactory.CreateSensor(type: SensorType.Moisture);
        var accountSensor = TestEntityFactory.CreateAccountSensor(sensor: sensor);
        mediator.AccountSensor = accountSensor;
        var moisture = new MeasurementMoistureEx(
            new MeasurementMoisture
            {
                DevEui = "test",
                Timestamp = DateTime.UtcNow,
                BatV = 3.3,
                RssiDbm = -90,
                SoilMoisturePrc = 50,
                SoilTemperature = 20,
                SoilConductivity = 100
            },
            accountSensor);
        mediator.Measurements = new IMeasurementEx[] { moisture };

        var result = await controller.Index("a", "s", graphType: GraphType.Temperature);

        var ok = Assert.IsType<OkObjectResult>(result);
        var measurementResult = Assert.IsType<MeasurementResult>(ok.Value);
        Assert.Equal("°C", measurementResult.Unit);
        var data = measurementResult.Data!.ToList();
        Assert.Equal(20.0, data[0].Value);
    }

    [Fact]
    public async Task Index_ReturnsOk_WithConductivityData()
    {
        var (controller, mediator) = CreateController();
        var sensor = TestEntityFactory.CreateSensor(type: SensorType.Moisture);
        var accountSensor = TestEntityFactory.CreateAccountSensor(sensor: sensor);
        mediator.AccountSensor = accountSensor;
        var moisture = new MeasurementMoistureEx(
            new MeasurementMoisture
            {
                DevEui = "test",
                Timestamp = DateTime.UtcNow,
                BatV = 3.3,
                RssiDbm = -90,
                SoilMoisturePrc = 50,
                SoilTemperature = 20,
                SoilConductivity = 100
            },
            accountSensor);
        mediator.Measurements = new IMeasurementEx[] { moisture };

        var result = await controller.Index("a", "s", graphType: GraphType.Conductivity);

        var ok = Assert.IsType<OkObjectResult>(result);
        var measurementResult = Assert.IsType<MeasurementResult>(ok.Value);
        Assert.Equal("µS/cm", measurementResult.Unit);
    }

    // --- Delete tests ---

    [Fact]
    public async Task Delete_ReturnsForbid_WhenNotAdmin()
    {
        var userInfo = new FakeUserInfo { Admin = false };
        var (controller, mediator) = CreateController(userInfo);

        var result = await controller.Delete("a", "s", DateTime.UtcNow);

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task Delete_ReturnsNotFound_WhenAccountSensorMissing()
    {
        var userInfo = new FakeUserInfo { Admin = true };
        var (controller, mediator) = CreateController(userInfo);

        var result = await controller.Delete("a", "s", DateTime.UtcNow);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Delete_ReturnsOk_WhenSuccessful()
    {
        var userInfo = new FakeUserInfo { Admin = true };
        var (controller, mediator) = CreateController(userInfo);
        var accountSensor = TestEntityFactory.CreateAccountSensor();
        mediator.AccountSensor = accountSensor;

        var result = await controller.Delete("a", "s", DateTime.UtcNow);

        Assert.IsType<OkObjectResult>(result);
        Assert.Contains(mediator.SentRequests, r => r is RemoveMeasurementCommand);
    }

    [Fact]
    public async Task Delete_ReturnsBadRequest_OnInvalidOperation()
    {
        var userInfo = new FakeUserInfo { Admin = true };
        var (controller, mediator) = CreateController(userInfo);
        var accountSensor = TestEntityFactory.CreateAccountSensor();
        mediator.AccountSensor = accountSensor;
        mediator.ThrowInvalidOperation = true;

        var result = await controller.Delete("a", "s", DateTime.UtcNow);

        Assert.IsType<BadRequestObjectResult>(result);
    }
}
