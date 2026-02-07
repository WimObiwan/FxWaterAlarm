using Core.Commands;
using Core.Communication;
using Core.Entities;
using Core.Helpers;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CoreTests.Commands;

public class CheckAllAccountSensorAlarmsCommandHandlerTest
{
    private class FakeMessenger : IMessenger
    {
        public List<(string Email, string Url, string? Name, string Message, string ShortMessage)> SentAlerts { get; } = [];

        public Task SendAuthenticationMailAsync(string emailAddress, string url, string code) =>
            Task.CompletedTask;

        public Task SendAlertMailAsync(string emailAddress, string url, string? accountSensorName,
            string alertMessage, string shortAlertMessage)
        {
            SentAlerts.Add((emailAddress, url, accountSensorName, alertMessage, shortAlertMessage));
            return Task.CompletedTask;
        }

        public Task SendLinkMailAsync(string emailAddress, string url) =>
            Task.CompletedTask;
    }

    private class FakeUrlBuilder : IUrlBuilder
    {
        public string BuildUrl(string? restPath = null) => $"https://test.example.com/{restPath}";
    }

    private class FakeMediator : IMediator
    {
        public object? NextResult { get; set; }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (NextResult is TResponse response)
                return Task.FromResult(response);
            return Task.FromResult(default(TResponse)!);
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
            => Task.CompletedTask;

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
            => Task.FromResult(NextResult);

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request,
            CancellationToken cancellationToken = default)
            => AsyncEnumerable.Empty<TResponse>();

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
            => AsyncEnumerable.Empty<object?>();

        public Task Publish(object notification, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification
            => Task.CompletedTask;
    }

    [Fact]
    public async Task Handle_NoAccountSensors_Succeeds()
    {
        await using var db = TestDbContext.Create();
        var handler = new CheckAllAccountSensorAlarmsCommandHandler(
            db.Context,
            new FakeMediator(),
            new FakeMessenger(),
            new FakeUrlBuilder(),
            NullLogger<CheckAllAccountSensorAlarmsCommandHandler>.Instance);

        // Should not throw
        await handler.Handle(new CheckAllAccountSensorAlarmsCommand(), CancellationToken.None);
    }

    [Fact]
    public async Task Handle_SkipsDisabledAccountSensors()
    {
        await using var db = TestDbContext.Create();
        // Create a disabled account sensor
        var (_, _, _) = await TestEntityFactory.SeedAccountWithSensor(db.Context,
            email: "chkall@test.com", accountLink: "chkalllink", sensorLink: "chkallsensor",
            disabled: true);

        var messenger = new FakeMessenger();
        var handler = new CheckAllAccountSensorAlarmsCommandHandler(
            db.Context,
            new FakeMediator(),
            messenger,
            new FakeUrlBuilder(),
            NullLogger<CheckAllAccountSensorAlarmsCommandHandler>.Instance);

        await handler.Handle(new CheckAllAccountSensorAlarmsCommand(), CancellationToken.None);

        Assert.Empty(messenger.SentAlerts);
    }

    [Fact]
    public async Task Handle_EnabledSensorWithoutAlerts_DoesNotSend()
    {
        await using var db = TestDbContext.Create();
        var (_, _, accountSensor) = await TestEntityFactory.SeedAccountWithSensor(db.Context,
            email: "chkall2@test.com", accountLink: "chkall2link", sensorLink: "chkall2sensor");

        // AlertsEnabled defaults to false, so even with an alarm, no alerts should be sent

        var messenger = new FakeMessenger();
        var handler = new CheckAllAccountSensorAlarmsCommandHandler(
            db.Context,
            new FakeMediator(),
            messenger,
            new FakeUrlBuilder(),
            NullLogger<CheckAllAccountSensorAlarmsCommandHandler>.Instance);

        await handler.Handle(new CheckAllAccountSensorAlarmsCommand(), CancellationToken.None);

        Assert.Empty(messenger.SentAlerts);
    }
}
