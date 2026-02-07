using Core.Commands;
using Core.Communication;
using Core.Entities;
using Core.Exceptions;
using Core.Helpers;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CoreTests.Commands;

public class CheckAccountSensorAlarmsCommandHandlerTest
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
    public async Task Handle_AccountSensorNotFound_ThrowsAccountSensorNotFoundException()
    {
        await using var db = TestDbContext.Create();
        var handler = new CheckAccountSensorAlarmsCommandHandler(
            db.Context,
            new FakeMediator(),
            new FakeMessenger(),
            new FakeUrlBuilder(),
            NullLogger<CheckAccountSensorAlarmsCommandHandler>.Instance);

        await Assert.ThrowsAsync<AccountSensorNotFoundException>(() =>
            handler.Handle(new CheckAccountSensorAlarmsCommand
            {
                AccountUid = Guid.NewGuid(),
                SensorUid = Guid.NewGuid()
            }, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_DisabledAccountSensor_ThrowsAccountSensorNotFoundException()
    {
        await using var db = TestDbContext.Create();
        var (account, sensor, _) = await TestEntityFactory.SeedAccountWithSensor(db.Context,
            email: "chkdis@test.com", accountLink: "chkdislink", sensorLink: "chkdissensor",
            disabled: true);

        var handler = new CheckAccountSensorAlarmsCommandHandler(
            db.Context,
            new FakeMediator(),
            new FakeMessenger(),
            new FakeUrlBuilder(),
            NullLogger<CheckAccountSensorAlarmsCommandHandler>.Instance);

        // The handler filters out disabled sensors, so it should throw NotFound
        await Assert.ThrowsAsync<AccountSensorNotFoundException>(() =>
            handler.Handle(new CheckAccountSensorAlarmsCommand
            {
                AccountUid = account.Uid,
                SensorUid = sensor.Uid
            }, CancellationToken.None));
    }

    [Fact]
    public async Task Handle_AlertsDisabled_DoesNotSendAlerts()
    {
        await using var db = TestDbContext.Create();
        var (account, sensor, accountSensor) = await TestEntityFactory.SeedAccountWithSensor(db.Context,
            email: "chknoa@test.com", accountLink: "chknoalink", sensorLink: "chknoasensor");

        // Load the Alarms collection so the backing field is populated by EF
        await db.Context.Entry(accountSensor).Collection(a => a.Alarms).LoadAsync();

        // AlertsEnabled defaults to false, add an alarm
        accountSensor.AddAlarm(new AccountSensorAlarm
        {
            Uid = Guid.NewGuid(),
            AlarmType = AccountSensorAlarmType.Data,
            AlarmThreshold = 24.5
        });
        await db.Context.SaveChangesAsync();

        var messenger = new FakeMessenger();
        var handler = new CheckAccountSensorAlarmsCommandHandler(
            db.Context,
            new FakeMediator(),
            messenger,
            new FakeUrlBuilder(),
            NullLogger<CheckAccountSensorAlarmsCommandHandler>.Instance);

        // Should not throw and not send alerts (AlertsEnabled is false)
        await handler.Handle(new CheckAccountSensorAlarmsCommand
        {
            AccountUid = account.Uid,
            SensorUid = sensor.Uid
        }, CancellationToken.None);

        Assert.Empty(messenger.SentAlerts);
    }
}
