using Core.Commands;
using Core.Communication;
using Core.Entities;
using Core.Exceptions;
using Core.Helpers;
using Core.Queries;
using Core.Util;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CoreTests.Commands;

/// <summary>
/// Comprehensive tests for CheckAccountSensorAlarmsCommandHandler and the base class
/// CheckAccountSensorAlarmsCommandHandlerBase covering all alarm type branches,
/// triggered/cleared logic, hysteresis, SendAlert overloads, and edge cases.
/// </summary>
public class CheckAccountSensorAlarmsCommandHandlerTest
{
    #region Fakes

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
        public string BuildUrl(string? restPath = null) => $"https://test.example.com{restPath}";
    }

    private class FakeMediator : IMediator
    {
        public Func<object, object?>? SendHandler { get; set; }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            var result = SendHandler?.Invoke(request);
            if (result is TResponse response)
                return Task.FromResult(response);
            return Task.FromResult(default(TResponse)!);
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
            => Task.CompletedTask;

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
            => Task.FromResult(SendHandler?.Invoke(request));

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

    #endregion

    #region Helpers

    /// <summary>
    /// Seeds an AccountSensor with AlertsEnabled=true, a single alarm, and returns all needed objects.
    /// DistanceMmEmpty=2000, DistanceMmFull=200 so LevelFraction and HeightMm compute correctly.
    /// </summary>
    private static async Task<(TestDbContext db, CheckAccountSensorAlarmsCommandHandler handler,
        FakeMessenger messenger, FakeMediator mediator, Account account, Sensor sensor)>
        SetupWithAlarm(
            AccountSensorAlarmType alarmType,
            double? alarmThreshold,
            SensorType sensorType = SensorType.Level,
            DateTime? lastTriggered = null,
            DateTime? lastCleared = null)
    {
        var db = TestDbContext.Create();
        var (account, sensor, accountSensor) = await TestEntityFactory.SeedAccountWithSensor(db.Context,
            email: $"chk-{Guid.NewGuid():N}@test.com",
            accountLink: $"al-{Guid.NewGuid():N}",
            sensorLink: $"sl-{Guid.NewGuid():N}",
            sensorType: sensorType);

        accountSensor.AlertsEnabled = true;
        accountSensor.DistanceMmEmpty = 2000;
        accountSensor.DistanceMmFull = 200;
        await db.Context.SaveChangesAsync();

        await db.Context.Entry(accountSensor).Collection(a => a.Alarms).LoadAsync();
        accountSensor.AddAlarm(new AccountSensorAlarm
        {
            Uid = Guid.NewGuid(),
            AlarmType = alarmType,
            AlarmThreshold = alarmThreshold,
            LastTriggered = lastTriggered,
            LastCleared = lastCleared
        });
        await db.Context.SaveChangesAsync();

        var messenger = new FakeMessenger();
        var mediator = new FakeMediator();
        var handler = new CheckAccountSensorAlarmsCommandHandler(
            db.Context, mediator, messenger, new FakeUrlBuilder(),
            NullLogger<CheckAccountSensorAlarmsCommandHandler>.Instance);

        return (db, handler, messenger, mediator, account, sensor);
    }

    private static MeasurementLevelEx CreateLevelMeasurement(AccountSensor accountSensor,
        DateTime timestamp, double batV = 3.6, int distanceMm = 1000)
    {
        return new MeasurementLevelEx(
            new MeasurementLevel
            {
                DevEui = accountSensor.Sensor.DevEui,
                Timestamp = timestamp,
                BatV = batV,
                RssiDbm = -80.0,
                DistanceMm = distanceMm
            },
            accountSensor);
    }

    private static MeasurementDetectEx CreateDetectMeasurement(AccountSensor accountSensor,
        DateTime timestamp, double batV = 3.6, int status = 0)
    {
        return new MeasurementDetectEx(
            new MeasurementDetect
            {
                DevEui = accountSensor.Sensor.DevEui,
                Timestamp = timestamp,
                BatV = batV,
                RssiDbm = -80.0,
                Status = status
            },
            accountSensor);
    }

    /// <summary>
    /// Runs the handler after configuring the mediator to return the given measurement.
    /// </summary>
    private static async Task RunHandler(
        CheckAccountSensorAlarmsCommandHandler handler,
        FakeMediator mediator,
        IMeasurementEx measurementEx,
        Account account,
        Sensor sensor)
    {
        mediator.SendHandler = request => request is LastMeasurementQuery ? measurementEx : null;

        await handler.Handle(new CheckAccountSensorAlarmsCommand
        {
            AccountUid = account.Uid,
            SensorUid = sensor.Uid
        }, CancellationToken.None);
    }

    /// <summary>Reloads alarms from the db to verify state changes.</summary>
    private static async Task<AccountSensorAlarm> GetAlarmFromDb(TestDbContext db)
    {
        var as2 = await db.Context.Set<AccountSensor>().SingleAsync();
        await db.Context.Entry(as2).Collection(a => a.Alarms).LoadAsync();
        return as2.Alarms.Single();
    }

    #endregion

    #region Basic flow tests

    [Fact]
    public async Task Handle_AccountSensorNotFound_ThrowsAccountSensorNotFoundException()
    {
        await using var db = TestDbContext.Create();
        var handler = new CheckAccountSensorAlarmsCommandHandler(
            db.Context, new FakeMediator(), new FakeMessenger(), new FakeUrlBuilder(),
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
            db.Context, new FakeMediator(), new FakeMessenger(), new FakeUrlBuilder(),
            NullLogger<CheckAccountSensorAlarmsCommandHandler>.Instance);

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

        await db.Context.Entry(accountSensor).Collection(a => a.Alarms).LoadAsync();
        accountSensor.AddAlarm(new AccountSensorAlarm
        {
            Uid = Guid.NewGuid(),
            AlarmType = AccountSensorAlarmType.Data,
            AlarmThreshold = 24.5
        });
        await db.Context.SaveChangesAsync();

        var messenger = new FakeMessenger();
        var handler = new CheckAccountSensorAlarmsCommandHandler(
            db.Context, new FakeMediator(), messenger, new FakeUrlBuilder(),
            NullLogger<CheckAccountSensorAlarmsCommandHandler>.Instance);

        await handler.Handle(new CheckAccountSensorAlarmsCommand
        {
            AccountUid = account.Uid,
            SensorUid = sensor.Uid
        }, CancellationToken.None);

        Assert.Empty(messenger.SentAlerts);
    }

    [Fact]
    public async Task Handle_NoAlarms_DoesNotSendAlerts()
    {
        await using var db = TestDbContext.Create();
        var (account, sensor, accountSensor) = await TestEntityFactory.SeedAccountWithSensor(db.Context,
            email: "chknoalarm@test.com", accountLink: "chknoalarmlink", sensorLink: "chknoalarmsensor");

        accountSensor.AlertsEnabled = true;
        await db.Context.SaveChangesAsync();

        var messenger = new FakeMessenger();
        var handler = new CheckAccountSensorAlarmsCommandHandler(
            db.Context, new FakeMediator(), messenger, new FakeUrlBuilder(),
            NullLogger<CheckAccountSensorAlarmsCommandHandler>.Instance);

        await handler.Handle(new CheckAccountSensorAlarmsCommand
        {
            AccountUid = account.Uid,
            SensorUid = sensor.Uid
        }, CancellationToken.None);

        Assert.Empty(messenger.SentAlerts);
    }

    [Fact]
    public async Task Handle_NoMeasurementReturned_ThrowsException()
    {
        var (db, handler, _, mediator, account, sensor) =
            await SetupWithAlarm(AccountSensorAlarmType.Data, 24.5);
        await using var _ = db;

        // Mediator returns null → "No measurement found"
        mediator.SendHandler = _ => null;

        await Assert.ThrowsAsync<Exception>(() =>
            handler.Handle(new CheckAccountSensorAlarmsCommand
            {
                AccountUid = account.Uid,
                SensorUid = sensor.Uid
            }, CancellationToken.None));
    }

    #endregion

    #region Data alarm

    [Fact]
    public async Task DataAlarm_OldTimestamp_TriggersAndSendsAlert()
    {
        var (db, handler, messenger, mediator, account, sensor) =
            await SetupWithAlarm(AccountSensorAlarmType.Data, 24.5);
        await using var _ = db;

        var accountSensor = await db.Context.Set<AccountSensor>()
            .Include(a => a.Sensor).Include(a => a.Account).SingleAsync();
        var measurement = CreateLevelMeasurement(accountSensor, DateTime.UtcNow.AddHours(-48));

        await RunHandler(handler, mediator, measurement, account, sensor);

        Assert.Single(messenger.SentAlerts);
        Assert.Contains("geen gegevens", messenger.SentAlerts[0].Message, StringComparison.OrdinalIgnoreCase);

        var alarm = await GetAlarmFromDb(db);
        Assert.True(alarm.IsCurrentlyTriggered);
    }

    [Fact]
    public async Task DataAlarm_RecentTimestamp_ClearsAlarm()
    {
        var (db, handler, messenger, mediator, account, sensor) =
            await SetupWithAlarm(AccountSensorAlarmType.Data, 24.5,
                lastTriggered: DateTime.UtcNow.AddHours(-1));
        await using var _ = db;

        var accountSensor = await db.Context.Set<AccountSensor>()
            .Include(a => a.Sensor).Include(a => a.Account).SingleAsync();
        var measurement = CreateLevelMeasurement(accountSensor, DateTime.UtcNow.AddMinutes(-5));

        await RunHandler(handler, mediator, measurement, account, sensor);

        // Data alarm has no sendAlertClearedFunction → no email sent
        Assert.Empty(messenger.SentAlerts);
        var alarm = await GetAlarmFromDb(db);
        Assert.True(alarm.IsCurrentlyCleared);
    }

    [Fact]
    public async Task DataAlarm_NullThreshold_DoesNotTrigger()
    {
        var (db, handler, messenger, mediator, account, sensor) =
            await SetupWithAlarm(AccountSensorAlarmType.Data, null);
        await using var _ = db;

        var accountSensor = await db.Context.Set<AccountSensor>()
            .Include(a => a.Sensor).Include(a => a.Account).SingleAsync();
        var measurement = CreateLevelMeasurement(accountSensor, DateTime.UtcNow.AddHours(-48));

        await RunHandler(handler, mediator, measurement, account, sensor);

        Assert.Empty(messenger.SentAlerts);
    }

    [Fact]
    public async Task DataAlarm_AlreadyTriggered_DoesNotSendAgain()
    {
        var (db, handler, messenger, mediator, account, sensor) =
            await SetupWithAlarm(AccountSensorAlarmType.Data, 24.5,
                lastTriggered: DateTime.UtcNow.AddMinutes(-10));
        await using var _ = db;

        var accountSensor = await db.Context.Set<AccountSensor>()
            .Include(a => a.Sensor).Include(a => a.Account).SingleAsync();
        var measurement = CreateLevelMeasurement(accountSensor, DateTime.UtcNow.AddHours(-48));

        await RunHandler(handler, mediator, measurement, account, sensor);

        Assert.Empty(messenger.SentAlerts);
    }

    [Fact]
    public async Task DataAlarm_AlreadyCleared_DoesNotClearAgain()
    {
        var (db, handler, messenger, mediator, account, sensor) =
            await SetupWithAlarm(AccountSensorAlarmType.Data, 24.5,
                lastCleared: DateTime.UtcNow.AddMinutes(-5));
        await using var _ = db;

        var accountSensor = await db.Context.Set<AccountSensor>()
            .Include(a => a.Sensor).Include(a => a.Account).SingleAsync();
        var measurement = CreateLevelMeasurement(accountSensor, DateTime.UtcNow.AddMinutes(-1));

        await RunHandler(handler, mediator, measurement, account, sensor);

        Assert.Empty(messenger.SentAlerts);
    }

    #endregion

    #region Battery alarm

    [Fact]
    public async Task BatteryAlarm_LowBattery_TriggersAndSendsAlert()
    {
        var (db, handler, messenger, mediator, account, sensor) =
            await SetupWithAlarm(AccountSensorAlarmType.Battery, 20.0);
        await using var _ = db;

        var accountSensor = await db.Context.Set<AccountSensor>()
            .Include(a => a.Sensor).Include(a => a.Account).SingleAsync();
        // BatV=3.0 → BatteryPrc = (3.0-3.0)/0.335*100 = 0% → below 20%
        var measurement = CreateLevelMeasurement(accountSensor, DateTime.UtcNow, batV: 3.0);

        await RunHandler(handler, mediator, measurement, account, sensor);

        Assert.Single(messenger.SentAlerts);
        Assert.Contains("batterij", messenger.SentAlerts[0].Message, StringComparison.OrdinalIgnoreCase);

        var alarm = await GetAlarmFromDb(db);
        Assert.True(alarm.IsCurrentlyTriggered);
    }

    [Fact]
    public async Task BatteryAlarm_HighBattery_AboveHysteresis_ClearsAlarm()
    {
        var (db, handler, messenger, mediator, account, sensor) =
            await SetupWithAlarm(AccountSensorAlarmType.Battery, 20.0,
                lastTriggered: DateTime.UtcNow.AddHours(-1));
        await using var _ = db;

        var accountSensor = await db.Context.Set<AccountSensor>()
            .Include(a => a.Sensor).Include(a => a.Account).SingleAsync();
        // Need BatteryPrc > 20 + 5 = 25%. BatV = 25*0.335/100+3.0 = 3.08375 → BatteryPrc ≈ 25%
        // Use 3.1 → BatteryPrc ≈ 29.85% > 25%
        var measurement = CreateLevelMeasurement(accountSensor, DateTime.UtcNow, batV: 3.1);

        await RunHandler(handler, mediator, measurement, account, sensor);

        // Battery alarm has no sendAlertClearedFunction
        Assert.Empty(messenger.SentAlerts);
        var alarm = await GetAlarmFromDb(db);
        Assert.True(alarm.IsCurrentlyCleared);
    }

    [Fact]
    public async Task BatteryAlarm_InHysteresisZone_NeitherTriggeredNorCleared()
    {
        var (db, handler, messenger, mediator, account, sensor) =
            await SetupWithAlarm(AccountSensorAlarmType.Battery, 20.0,
                lastTriggered: DateTime.UtcNow.AddHours(-1));
        await using var _ = db;

        var accountSensor = await db.Context.Set<AccountSensor>()
            .Include(a => a.Sensor).Include(a => a.Account).SingleAsync();
        // BatV = 3.075 → BatteryPrc ≈ 22.4%. Above 20 (not triggered), but below 25 (not cleared).
        var measurement = CreateLevelMeasurement(accountSensor, DateTime.UtcNow, batV: 3.075);

        await RunHandler(handler, mediator, measurement, account, sensor);

        Assert.Empty(messenger.SentAlerts);
        // Alarm stays in triggered state
        var alarm = await GetAlarmFromDb(db);
        Assert.True(alarm.IsCurrentlyTriggered);
    }

    [Fact]
    public async Task BatteryAlarm_NullThreshold_DoesNotTrigger()
    {
        var (db, handler, messenger, mediator, account, sensor) =
            await SetupWithAlarm(AccountSensorAlarmType.Battery, null);
        await using var _ = db;

        var accountSensor = await db.Context.Set<AccountSensor>()
            .Include(a => a.Sensor).Include(a => a.Account).SingleAsync();
        var measurement = CreateLevelMeasurement(accountSensor, DateTime.UtcNow, batV: 3.0);

        await RunHandler(handler, mediator, measurement, account, sensor);

        Assert.Empty(messenger.SentAlerts);
    }

    #endregion

    #region PercentageLow alarm

    [Fact]
    public async Task PercentageLowAlarm_LowLevel_TriggersAndSendsAlert()
    {
        var (db, handler, messenger, mediator, account, sensor) =
            await SetupWithAlarm(AccountSensorAlarmType.PercentageLow, 25.0);
        await using var _ = db;

        var accountSensor = await db.Context.Set<AccountSensor>()
            .Include(a => a.Sensor).Include(a => a.Account).SingleAsync();
        // DistanceMm=1800 → LevelFraction=(2000-1800)/(2000-200)=200/1800≈0.111 → 11.1% ≤ 25%
        var measurement = CreateLevelMeasurement(accountSensor, DateTime.UtcNow, distanceMm: 1800);

        await RunHandler(handler, mediator, measurement, account, sensor);

        Assert.Single(messenger.SentAlerts);
        Assert.Contains("gezakt", messenger.SentAlerts[0].Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("niveau", messenger.SentAlerts[0].Message, StringComparison.OrdinalIgnoreCase);

        var alarm = await GetAlarmFromDb(db);
        Assert.True(alarm.IsCurrentlyTriggered);
    }

    [Fact]
    public async Task PercentageLowAlarm_HighLevel_ClearsAlarm()
    {
        var (db, handler, _, mediator, account, sensor) =
            await SetupWithAlarm(AccountSensorAlarmType.PercentageLow, 25.0,
                lastTriggered: DateTime.UtcNow.AddHours(-1));
        await using var _ = db;

        var accountSensor = await db.Context.Set<AccountSensor>()
            .Include(a => a.Sensor).Include(a => a.Account).SingleAsync();
        // DistanceMm=500 → LevelFraction=(2000-500)/1800≈0.833 → 83.3% > 25+5=30%
        var measurement = CreateLevelMeasurement(accountSensor, DateTime.UtcNow, distanceMm: 500);

        await RunHandler(handler, mediator, measurement, account, sensor);

        var alarm = await GetAlarmFromDb(db);
        Assert.True(alarm.IsCurrentlyCleared);
    }

    [Fact]
    public async Task PercentageLowAlarm_NonLevelMeasurement_DoesNotTrigger()
    {
        var (db, handler, messenger, mediator, account, sensor) =
            await SetupWithAlarm(AccountSensorAlarmType.PercentageLow, 25.0,
                sensorType: SensorType.Detect);
        await using var _ = db;

        var accountSensor = await db.Context.Set<AccountSensor>()
            .Include(a => a.Sensor).Include(a => a.Account).SingleAsync();
        var measurement = CreateDetectMeasurement(accountSensor, DateTime.UtcNow, status: 0);

        await RunHandler(handler, mediator, measurement, account, sensor);

        Assert.Empty(messenger.SentAlerts);
    }

    [Fact]
    public async Task PercentageLowAlarm_NullLevelFraction_DoesNotTrigger()
    {
        var (db, handler, messenger, mediator, account, sensor) =
            await SetupWithAlarm(AccountSensorAlarmType.PercentageLow, 25.0);
        await using var _ = db;

        var accountSensor = await db.Context.Set<AccountSensor>()
            .Include(a => a.Sensor).Include(a => a.Account).SingleAsync();
        // Remove distance config so LevelFraction returns null
        accountSensor.DistanceMmEmpty = null;
        accountSensor.DistanceMmFull = null;
        await db.Context.SaveChangesAsync();

        var measurement = CreateLevelMeasurement(accountSensor, DateTime.UtcNow, distanceMm: 1000);

        await RunHandler(handler, mediator, measurement, account, sensor);

        Assert.Empty(messenger.SentAlerts);
    }

    [Fact]
    public async Task PercentageLowAlarm_NullThreshold_DoesNotTrigger()
    {
        var (db, handler, messenger, mediator, account, sensor) =
            await SetupWithAlarm(AccountSensorAlarmType.PercentageLow, null);
        await using var _ = db;

        var accountSensor = await db.Context.Set<AccountSensor>()
            .Include(a => a.Sensor).Include(a => a.Account).SingleAsync();
        var measurement = CreateLevelMeasurement(accountSensor, DateTime.UtcNow, distanceMm: 1800);

        await RunHandler(handler, mediator, measurement, account, sensor);

        Assert.Empty(messenger.SentAlerts);
    }

    #endregion

    #region PercentageHigh alarm

    [Fact]
    public async Task PercentageHighAlarm_HighLevel_TriggersAndSendsAlert()
    {
        var (db, handler, messenger, mediator, account, sensor) =
            await SetupWithAlarm(AccountSensorAlarmType.PercentageHigh, 80.0);
        await using var _ = db;

        var accountSensor = await db.Context.Set<AccountSensor>()
            .Include(a => a.Sensor).Include(a => a.Account).SingleAsync();
        // DistanceMm=300 → LevelFraction=(2000-300)/1800≈0.944 → 94.4% ≥ 80%
        var measurement = CreateLevelMeasurement(accountSensor, DateTime.UtcNow, distanceMm: 300);

        await RunHandler(handler, mediator, measurement, account, sensor);

        Assert.Single(messenger.SentAlerts);
        Assert.Contains("gestegen", messenger.SentAlerts[0].Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PercentageHighAlarm_LowLevel_ClearsAlarm()
    {
        var (db, handler, _, mediator, account, sensor) =
            await SetupWithAlarm(AccountSensorAlarmType.PercentageHigh, 80.0,
                lastTriggered: DateTime.UtcNow.AddHours(-1));
        await using var _ = db;

        var accountSensor = await db.Context.Set<AccountSensor>()
            .Include(a => a.Sensor).Include(a => a.Account).SingleAsync();
        // DistanceMm=1800 → LevelFraction=11.1% < 80-5=75%
        var measurement = CreateLevelMeasurement(accountSensor, DateTime.UtcNow, distanceMm: 1800);

        await RunHandler(handler, mediator, measurement, account, sensor);

        var alarm = await GetAlarmFromDb(db);
        Assert.True(alarm.IsCurrentlyCleared);
    }

    [Fact]
    public async Task PercentageHighAlarm_NonLevelMeasurement_DoesNotTrigger()
    {
        var (db, handler, messenger, mediator, account, sensor) =
            await SetupWithAlarm(AccountSensorAlarmType.PercentageHigh, 80.0,
                sensorType: SensorType.Detect);
        await using var _ = db;

        var accountSensor = await db.Context.Set<AccountSensor>()
            .Include(a => a.Sensor).Include(a => a.Account).SingleAsync();
        var measurement = CreateDetectMeasurement(accountSensor, DateTime.UtcNow);

        await RunHandler(handler, mediator, measurement, account, sensor);

        Assert.Empty(messenger.SentAlerts);
    }

    [Fact]
    public async Task PercentageHighAlarm_NullLevelFraction_DoesNotTrigger()
    {
        var (db, handler, messenger, mediator, account, sensor) =
            await SetupWithAlarm(AccountSensorAlarmType.PercentageHigh, 80.0);
        await using var _ = db;

        var accountSensor = await db.Context.Set<AccountSensor>()
            .Include(a => a.Sensor).Include(a => a.Account).SingleAsync();
        accountSensor.DistanceMmEmpty = null;
        accountSensor.DistanceMmFull = null;
        await db.Context.SaveChangesAsync();

        var measurement = CreateLevelMeasurement(accountSensor, DateTime.UtcNow, distanceMm: 300);

        await RunHandler(handler, mediator, measurement, account, sensor);

        Assert.Empty(messenger.SentAlerts);
    }

    [Fact]
    public async Task PercentageHighAlarm_NullThreshold_DoesNotTrigger()
    {
        var (db, handler, messenger, mediator, account, sensor) =
            await SetupWithAlarm(AccountSensorAlarmType.PercentageHigh, null);
        await using var _ = db;

        var accountSensor = await db.Context.Set<AccountSensor>()
            .Include(a => a.Sensor).Include(a => a.Account).SingleAsync();
        var measurement = CreateLevelMeasurement(accountSensor, DateTime.UtcNow, distanceMm: 300);

        await RunHandler(handler, mediator, measurement, account, sensor);

        Assert.Empty(messenger.SentAlerts);
    }

    #endregion

    #region HeightLow alarm

    [Fact]
    public async Task HeightLowAlarm_LowHeight_TriggersAndSendsAlert()
    {
        var (db, handler, messenger, mediator, account, sensor) =
            await SetupWithAlarm(AccountSensorAlarmType.HeightLow, 500.0);
        await using var _ = db;

        var accountSensor = await db.Context.Set<AccountSensor>()
            .Include(a => a.Sensor).Include(a => a.Account).SingleAsync();
        // DistanceMm=1800, DistanceMmEmpty=2000 → HeightMm = 2000-1800 = 200 ≤ 500
        var measurement = CreateLevelMeasurement(accountSensor, DateTime.UtcNow, distanceMm: 1800);

        await RunHandler(handler, mediator, measurement, account, sensor);

        Assert.Single(messenger.SentAlerts);
        Assert.Contains("gezakt", messenger.SentAlerts[0].Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("mm", messenger.SentAlerts[0].ShortMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HeightLowAlarm_HighHeight_AboveHysteresis_ClearsAlarm()
    {
        var (db, handler, _, mediator, account, sensor) =
            await SetupWithAlarm(AccountSensorAlarmType.HeightLow, 500.0,
                lastTriggered: DateTime.UtcNow.AddHours(-1));
        await using var _ = db;

        var accountSensor = await db.Context.Set<AccountSensor>()
            .Include(a => a.Sensor).Include(a => a.Account).SingleAsync();
        // DistanceMm=300 → HeightMm = 2000-300 = 1700 > 500+50 = 550
        var measurement = CreateLevelMeasurement(accountSensor, DateTime.UtcNow, distanceMm: 300);

        await RunHandler(handler, mediator, measurement, account, sensor);

        var alarm = await GetAlarmFromDb(db);
        Assert.True(alarm.IsCurrentlyCleared);
    }

    [Fact]
    public async Task HeightLowAlarm_NonLevelMeasurement_DoesNotTrigger()
    {
        var (db, handler, messenger, mediator, account, sensor) =
            await SetupWithAlarm(AccountSensorAlarmType.HeightLow, 500.0,
                sensorType: SensorType.Detect);
        await using var _ = db;

        var accountSensor = await db.Context.Set<AccountSensor>()
            .Include(a => a.Sensor).Include(a => a.Account).SingleAsync();
        var measurement = CreateDetectMeasurement(accountSensor, DateTime.UtcNow);

        await RunHandler(handler, mediator, measurement, account, sensor);

        Assert.Empty(messenger.SentAlerts);
    }

    [Fact]
    public async Task HeightLowAlarm_NullHeightMm_DoesNotTrigger()
    {
        var (db, handler, messenger, mediator, account, sensor) =
            await SetupWithAlarm(AccountSensorAlarmType.HeightLow, 500.0);
        await using var _ = db;

        var accountSensor = await db.Context.Set<AccountSensor>()
            .Include(a => a.Sensor).Include(a => a.Account).SingleAsync();
        // Remove DistanceMmEmpty so HeightMm returns null
        accountSensor.DistanceMmEmpty = null;
        await db.Context.SaveChangesAsync();

        var measurement = CreateLevelMeasurement(accountSensor, DateTime.UtcNow, distanceMm: 1800);

        await RunHandler(handler, mediator, measurement, account, sensor);

        Assert.Empty(messenger.SentAlerts);
    }

    [Fact]
    public async Task HeightLowAlarm_NullThreshold_DoesNotTrigger()
    {
        var (db, handler, messenger, mediator, account, sensor) =
            await SetupWithAlarm(AccountSensorAlarmType.HeightLow, null);
        await using var _ = db;

        var accountSensor = await db.Context.Set<AccountSensor>()
            .Include(a => a.Sensor).Include(a => a.Account).SingleAsync();
        var measurement = CreateLevelMeasurement(accountSensor, DateTime.UtcNow, distanceMm: 1800);

        await RunHandler(handler, mediator, measurement, account, sensor);

        Assert.Empty(messenger.SentAlerts);
    }

    #endregion

    #region HeightHigh alarm

    [Fact]
    public async Task HeightHighAlarm_HighHeight_TriggersAndSendsAlert()
    {
        var (db, handler, messenger, mediator, account, sensor) =
            await SetupWithAlarm(AccountSensorAlarmType.HeightHigh, 1500.0);
        await using var _ = db;

        var accountSensor = await db.Context.Set<AccountSensor>()
            .Include(a => a.Sensor).Include(a => a.Account).SingleAsync();
        // DistanceMm=300 → HeightMm = 2000-300 = 1700 ≥ 1500
        var measurement = CreateLevelMeasurement(accountSensor, DateTime.UtcNow, distanceMm: 300);

        await RunHandler(handler, mediator, measurement, account, sensor);

        Assert.Single(messenger.SentAlerts);
        Assert.Contains("gestegen", messenger.SentAlerts[0].Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("mm", messenger.SentAlerts[0].ShortMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HeightHighAlarm_LowHeight_BelowHysteresis_ClearsAlarm()
    {
        var (db, handler, messenger, mediator, account, sensor) =
            await SetupWithAlarm(AccountSensorAlarmType.HeightHigh, 1500.0,
                lastTriggered: DateTime.UtcNow.AddHours(-1));
        await using var _ = db;

        var accountSensor = await db.Context.Set<AccountSensor>()
            .Include(a => a.Sensor).Include(a => a.Account).SingleAsync();
        // DistanceMm=1800 → HeightMm = 2000-1800 = 200 < 1500-50 = 1450
        var measurement = CreateLevelMeasurement(accountSensor, DateTime.UtcNow, distanceMm: 1800);

        await RunHandler(handler, mediator, measurement, account, sensor);

        var alarm = await GetAlarmFromDb(db);
        Assert.True(alarm.IsCurrentlyCleared);
    }

    [Fact]
    public async Task HeightHighAlarm_NonLevelMeasurement_DoesNotTrigger()
    {
        var (db, handler, messenger, mediator, account, sensor) =
            await SetupWithAlarm(AccountSensorAlarmType.HeightHigh, 1500.0,
                sensorType: SensorType.Detect);
        await using var _ = db;

        var accountSensor = await db.Context.Set<AccountSensor>()
            .Include(a => a.Sensor).Include(a => a.Account).SingleAsync();
        var measurement = CreateDetectMeasurement(accountSensor, DateTime.UtcNow);

        await RunHandler(handler, mediator, measurement, account, sensor);

        Assert.Empty(messenger.SentAlerts);
    }

    [Fact]
    public async Task HeightHighAlarm_NullHeightMm_DoesNotTrigger()
    {
        var (db, handler, messenger, mediator, account, sensor) =
            await SetupWithAlarm(AccountSensorAlarmType.HeightHigh, 1500.0);
        await using var _ = db;

        var accountSensor = await db.Context.Set<AccountSensor>()
            .Include(a => a.Sensor).Include(a => a.Account).SingleAsync();
        accountSensor.DistanceMmEmpty = null;
        await db.Context.SaveChangesAsync();

        var measurement = CreateLevelMeasurement(accountSensor, DateTime.UtcNow, distanceMm: 300);

        await RunHandler(handler, mediator, measurement, account, sensor);

        Assert.Empty(messenger.SentAlerts);
    }

    [Fact]
    public async Task HeightHighAlarm_NullThreshold_DoesNotTrigger()
    {
        var (db, handler, messenger, mediator, account, sensor) =
            await SetupWithAlarm(AccountSensorAlarmType.HeightHigh, null);
        await using var _ = db;

        var accountSensor = await db.Context.Set<AccountSensor>()
            .Include(a => a.Sensor).Include(a => a.Account).SingleAsync();
        var measurement = CreateLevelMeasurement(accountSensor, DateTime.UtcNow, distanceMm: 300);

        await RunHandler(handler, mediator, measurement, account, sensor);

        Assert.Empty(messenger.SentAlerts);
    }

    #endregion

    #region DetectOn alarm

    [Fact]
    public async Task DetectOnAlarm_StatusOne_TriggersAndSendsAlert()
    {
        var (db, handler, messenger, mediator, account, sensor) =
            await SetupWithAlarm(AccountSensorAlarmType.DetectOn, null,
                sensorType: SensorType.Detect);
        await using var _ = db;

        var accountSensor = await db.Context.Set<AccountSensor>()
            .Include(a => a.Sensor).Include(a => a.Account).SingleAsync();
        var measurement = CreateDetectMeasurement(accountSensor, DateTime.UtcNow, status: 1);

        await RunHandler(handler, mediator, measurement, account, sensor);

        Assert.Single(messenger.SentAlerts);
        Assert.Contains("water gedetecteerd", messenger.SentAlerts[0].Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DetectOnAlarm_StatusZero_ClearsAndSendsAlertCleared()
    {
        var (db, handler, messenger, mediator, account, sensor) =
            await SetupWithAlarm(AccountSensorAlarmType.DetectOn, null,
                sensorType: SensorType.Detect,
                lastTriggered: DateTime.UtcNow.AddHours(-1));
        await using var _ = db;

        var accountSensor = await db.Context.Set<AccountSensor>()
            .Include(a => a.Sensor).Include(a => a.Account).SingleAsync();
        var measurement = CreateDetectMeasurement(accountSensor, DateTime.UtcNow, status: 0);

        await RunHandler(handler, mediator, measurement, account, sensor);

        // DetectOn has sendAlertClearedFunction → email sent
        Assert.Single(messenger.SentAlerts);
        Assert.Contains("geen water", messenger.SentAlerts[0].Message, StringComparison.OrdinalIgnoreCase);

        var alarm = await GetAlarmFromDb(db);
        Assert.True(alarm.IsCurrentlyCleared);
    }

    [Fact]
    public async Task DetectOnAlarm_NonDetectMeasurement_DoesNotTrigger()
    {
        var (db, handler, messenger, mediator, account, sensor) =
            await SetupWithAlarm(AccountSensorAlarmType.DetectOn, null,
                sensorType: SensorType.Level);
        await using var _ = db;

        var accountSensor = await db.Context.Set<AccountSensor>()
            .Include(a => a.Sensor).Include(a => a.Account).SingleAsync();
        var measurement = CreateLevelMeasurement(accountSensor, DateTime.UtcNow);

        await RunHandler(handler, mediator, measurement, account, sensor);

        Assert.Empty(messenger.SentAlerts);
    }

    [Fact]
    public async Task DetectOnAlarm_AlreadyTriggered_DoesNotSendAgain()
    {
        var (db, handler, messenger, mediator, account, sensor) =
            await SetupWithAlarm(AccountSensorAlarmType.DetectOn, null,
                sensorType: SensorType.Detect,
                lastTriggered: DateTime.UtcNow.AddMinutes(-10));
        await using var _ = db;

        var accountSensor = await db.Context.Set<AccountSensor>()
            .Include(a => a.Sensor).Include(a => a.Account).SingleAsync();
        var measurement = CreateDetectMeasurement(accountSensor, DateTime.UtcNow, status: 1);

        await RunHandler(handler, mediator, measurement, account, sensor);

        Assert.Empty(messenger.SentAlerts);
    }

    [Fact]
    public async Task DetectOnAlarm_AlreadyCleared_DoesNotSendAgain()
    {
        var (db, handler, messenger, mediator, account, sensor) =
            await SetupWithAlarm(AccountSensorAlarmType.DetectOn, null,
                sensorType: SensorType.Detect,
                lastCleared: DateTime.UtcNow.AddMinutes(-5));
        await using var _ = db;

        var accountSensor = await db.Context.Set<AccountSensor>()
            .Include(a => a.Sensor).Include(a => a.Account).SingleAsync();
        var measurement = CreateDetectMeasurement(accountSensor, DateTime.UtcNow, status: 0);

        await RunHandler(handler, mediator, measurement, account, sensor);

        Assert.Empty(messenger.SentAlerts);
    }

    #endregion

    #region Default / unknown alarm type

    [Fact]
    public async Task UnknownAlarmType_DoesNotTriggerOrClear()
    {
        var (db, handler, messenger, mediator, account, sensor) =
            await SetupWithAlarm((AccountSensorAlarmType)999, 10.0);
        await using var _ = db;

        var accountSensor = await db.Context.Set<AccountSensor>()
            .Include(a => a.Sensor).Include(a => a.Account).SingleAsync();
        var measurement = CreateLevelMeasurement(accountSensor, DateTime.UtcNow);

        await RunHandler(handler, mediator, measurement, account, sensor);

        Assert.Empty(messenger.SentAlerts);
    }

    #endregion

    #region SendAlert message and url verification

    [Fact]
    public async Task SendAlert_IncludesCorrectEmailAndRestPathUrl()
    {
        var (db, handler, messenger, mediator, account, sensor) =
            await SetupWithAlarm(AccountSensorAlarmType.Data, 1.0);
        await using var _ = db;

        var accountSensor = await db.Context.Set<AccountSensor>()
            .Include(a => a.Sensor).Include(a => a.Account).SingleAsync();
        // More than 1 hour old
        var measurement = CreateLevelMeasurement(accountSensor, DateTime.UtcNow.AddHours(-5));

        await RunHandler(handler, mediator, measurement, account, sensor);

        Assert.Single(messenger.SentAlerts);
        var alert = messenger.SentAlerts[0];
        Assert.Equal(account.Email, alert.Email);
        Assert.Contains("test.example.com", alert.Url);
    }

    [Fact]
    public async Task SendAlert_Battery_ShortMessageContainsPercentage()
    {
        var (db, handler, messenger, mediator, account, sensor) =
            await SetupWithAlarm(AccountSensorAlarmType.Battery, 50.0);
        await using var _ = db;

        var accountSensor = await db.Context.Set<AccountSensor>()
            .Include(a => a.Sensor).Include(a => a.Account).SingleAsync();
        // BatV=3.1 → BatteryPrc ≈ 29.85% ≤ 50%
        var measurement = CreateLevelMeasurement(accountSensor, DateTime.UtcNow, batV: 3.1);

        await RunHandler(handler, mediator, measurement, account, sensor);

        Assert.Single(messenger.SentAlerts);
        Assert.Contains("Batterij", messenger.SentAlerts[0].ShortMessage);
        Assert.Contains("%", messenger.SentAlerts[0].ShortMessage);
    }

    [Fact]
    public async Task SendAlert_PercentageLow_ShortMessageContainsNiveau()
    {
        var (db, handler, messenger, mediator, account, sensor) =
            await SetupWithAlarm(AccountSensorAlarmType.PercentageLow, 50.0);
        await using var _ = db;

        var accountSensor = await db.Context.Set<AccountSensor>()
            .Include(a => a.Sensor).Include(a => a.Account).SingleAsync();
        // DistanceMm=1800 → LevelFraction≈11.1% ≤ 50%
        var measurement = CreateLevelMeasurement(accountSensor, DateTime.UtcNow, distanceMm: 1800);

        await RunHandler(handler, mediator, measurement, account, sensor);

        Assert.Single(messenger.SentAlerts);
        Assert.Contains("Niveau", messenger.SentAlerts[0].ShortMessage);
    }

    [Fact]
    public async Task DetectOn_Triggered_ShortMessageContainsWater()
    {
        var (db, handler, messenger, mediator, account, sensor) =
            await SetupWithAlarm(AccountSensorAlarmType.DetectOn, null,
                sensorType: SensorType.Detect);
        await using var _ = db;

        var accountSensor = await db.Context.Set<AccountSensor>()
            .Include(a => a.Sensor).Include(a => a.Account).SingleAsync();
        var measurement = CreateDetectMeasurement(accountSensor, DateTime.UtcNow, status: 1);

        await RunHandler(handler, mediator, measurement, account, sensor);

        Assert.Single(messenger.SentAlerts);
        Assert.Contains("Water gedetecteerd", messenger.SentAlerts[0].ShortMessage);
    }

    [Fact]
    public async Task DetectOn_Cleared_ShortMessageContainsGeenWater()
    {
        var (db, handler, messenger, mediator, account, sensor) =
            await SetupWithAlarm(AccountSensorAlarmType.DetectOn, null,
                sensorType: SensorType.Detect,
                lastTriggered: DateTime.UtcNow.AddHours(-1));
        await using var _ = db;

        var accountSensor = await db.Context.Set<AccountSensor>()
            .Include(a => a.Sensor).Include(a => a.Account).SingleAsync();
        var measurement = CreateDetectMeasurement(accountSensor, DateTime.UtcNow, status: 0);

        await RunHandler(handler, mediator, measurement, account, sensor);

        Assert.Single(messenger.SentAlerts);
        Assert.Contains("Geen water gedetecteerd", messenger.SentAlerts[0].ShortMessage);
    }

    #endregion
}
