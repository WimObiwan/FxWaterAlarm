using Core.Commands;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Site.Controllers;
using Xunit;

namespace SiteTests.Controllers;

public class DevEuiControllerTest
{
    private class FakeMediator : IMediator
    {
        public bool ThrowInvalidOperation { get; set; }
        public bool ThrowArgument { get; set; }
        public bool ThrowNotSupported { get; set; }
        public bool ThrowGeneral { get; set; }
        public AddMeasurementCommand? ReceivedCommand { get; private set; }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
            => Task.FromResult(default(TResponse)!);

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
        {
            if (request is AddMeasurementCommand cmd)
            {
                ReceivedCommand = cmd;
                if (ThrowInvalidOperation) throw new InvalidOperationException("Sensor not found");
                if (ThrowArgument) throw new ArgumentException("Bad argument");
                if (ThrowNotSupported) throw new NotSupportedException("Not supported");
                if (ThrowGeneral) throw new Exception("Unexpected error");
            }
            return Task.CompletedTask;
        }

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

    private static DevEuiController CreateController(FakeMediator mediator)
    {
        var controller = new DevEuiController(mediator);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        return controller;
    }

    [Fact]
    public async Task AddMeasurement_ReturnsOk_OnSuccess()
    {
        var mediator = new FakeMediator();
        var controller = CreateController(mediator);
        var request = new AddMeasurementRequest
        {
            Timestamp = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc),
            Measurements = new Dictionary<string, object> { { "distance_mm", 1000 } }
        };

        var result = await controller.AddMeasurement("test-dev-eui", request);

        Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(mediator.ReceivedCommand);
        Assert.Equal("test-dev-eui", mediator.ReceivedCommand!.DevEui);
    }

    [Fact]
    public async Task AddMeasurement_UsesUtcNow_WhenTimestampIsDefault()
    {
        var mediator = new FakeMediator();
        var controller = CreateController(mediator);
        var request = new AddMeasurementRequest
        {
            Timestamp = default,
            Measurements = new Dictionary<string, object> { { "distance_mm", 1000 } }
        };

        var before = DateTime.UtcNow;
        await controller.AddMeasurement("dev-eui", request);
        var after = DateTime.UtcNow;

        Assert.InRange(mediator.ReceivedCommand!.Timestamp, before, after);
    }

    [Fact]
    public async Task AddMeasurement_ReturnsBadRequest_OnInvalidOperation()
    {
        var mediator = new FakeMediator { ThrowInvalidOperation = true };
        var controller = CreateController(mediator);
        var request = new AddMeasurementRequest
        {
            Timestamp = DateTime.UtcNow,
            Measurements = new Dictionary<string, object>()
        };

        var result = await controller.AddMeasurement("dev-eui", request);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequest.Value);
    }

    [Fact]
    public async Task AddMeasurement_ReturnsBadRequest_OnArgumentException()
    {
        var mediator = new FakeMediator { ThrowArgument = true };
        var controller = CreateController(mediator);
        var request = new AddMeasurementRequest
        {
            Timestamp = DateTime.UtcNow,
            Measurements = new Dictionary<string, object>()
        };

        var result = await controller.AddMeasurement("dev-eui", request);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task AddMeasurement_ReturnsBadRequest_OnNotSupported()
    {
        var mediator = new FakeMediator { ThrowNotSupported = true };
        var controller = CreateController(mediator);
        var request = new AddMeasurementRequest
        {
            Timestamp = DateTime.UtcNow,
            Measurements = new Dictionary<string, object>()
        };

        var result = await controller.AddMeasurement("dev-eui", request);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task AddMeasurement_Returns500_OnUnexpectedException()
    {
        var mediator = new FakeMediator { ThrowGeneral = true };
        var controller = CreateController(mediator);
        var request = new AddMeasurementRequest
        {
            Timestamp = DateTime.UtcNow,
            Measurements = new Dictionary<string, object>()
        };

        var result = await controller.AddMeasurement("dev-eui", request);

        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    [Fact]
    public async Task AddMeasurement_PassesMeasurements()
    {
        var mediator = new FakeMediator();
        var controller = CreateController(mediator);
        var measurements = new Dictionary<string, object>
        {
            { "distance_mm", 500 },
            { "bat_v", 3.3 }
        };
        var request = new AddMeasurementRequest
        {
            Timestamp = DateTime.UtcNow,
            Measurements = measurements
        };

        await controller.AddMeasurement("dev-eui", request);

        Assert.Equal(2, mediator.ReceivedCommand!.Measurements.Count);
    }
}
