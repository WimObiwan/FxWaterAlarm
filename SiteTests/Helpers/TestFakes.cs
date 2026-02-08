using Core.Commands;
using Core.Communication;
using Core.Entities;
using Core.Helpers;
using Core.Queries;
using Core.Util;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Routing;
using Site.Services;
using Site.Utilities;

namespace SiteTests.Helpers;

/// <summary>
/// A configurable IMediator fake that returns pre-registered responses for queries
/// and captures sent commands. 
/// </summary>
public class ConfigurableFakeMediator : IMediator
{
    private readonly Dictionary<Type, object?> _responses = new();
    private readonly List<object> _sentRequests = new();
    public bool ThrowOnSend { get; set; }
    public Exception? ExceptionToThrow { get; set; }

    public IReadOnlyList<object> SentRequests => _sentRequests.AsReadOnly();

    public void SetResponse<TRequest, TResponse>(TResponse response) where TRequest : IRequest<TResponse>
    {
        _responses[typeof(TRequest)] = response;
    }

    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        _sentRequests.Add(request);
        if (ThrowOnSend && ExceptionToThrow != null) throw ExceptionToThrow;
        if (_responses.TryGetValue(request.GetType(), out var response))
            return Task.FromResult((TResponse)response!);
        return Task.FromResult(default(TResponse)!);
    }

    public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
    {
        _sentRequests.Add(request!);
        if (ThrowOnSend && ExceptionToThrow != null) throw ExceptionToThrow;
        return Task.CompletedTask;
    }

    public Task<object?> Send(object request, CancellationToken cancellationToken = default)
    {
        _sentRequests.Add(request);
        return Task.FromResult<object?>(null);
    }

    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
        => AsyncEnumerable.Empty<TResponse>();

    public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
        => AsyncEnumerable.Empty<object?>();

    public Task Publish(object notification, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification
        => Task.CompletedTask;
}

public class FakeUserInfo : IUserInfo
{
    public bool Authenticated { get; set; }
    public string? LoginEmail { get; set; }
    public bool Admin { get; set; }
    public bool CanUpdate { get; set; } = true;

    public bool IsAuthenticated() => Authenticated;
    public string? GetLoginEmail() => LoginEmail;
    public Task<bool> CanUpdateAccount(Account account) => Task.FromResult(CanUpdate);
    public Task<bool> CanUpdateAccountSensor(AccountSensor accountSensor) => Task.FromResult(CanUpdate);
    public Task<bool> IsAdmin() => Task.FromResult(Admin);
}

public class FakeUrlBuilder : IUrlBuilder
{
    public string BuildUrl(string? restPath = null) => $"https://test.example.com{restPath}";
}

public class FakeMessenger : IMessenger
{
    public List<(string Email, string Url, string Code)> AuthMails { get; } = new();
    public List<(string Email, string Url, string? Name, string Message, string Short)> AlertMails { get; } = new();
    public List<(string Email, string Url)> LinkMails { get; } = new();

    public Task SendAuthenticationMailAsync(string emailAddress, string url, string code)
    {
        AuthMails.Add((emailAddress, url, code));
        return Task.CompletedTask;
    }

    public Task SendAlertMailAsync(string emailAddress, string url, string? accountSensorName, string alertMessage, string shortAlertMessage)
    {
        AlertMails.Add((emailAddress, url, accountSensorName, alertMessage, shortAlertMessage));
        return Task.CompletedTask;
    }

    public Task SendLinkMailAsync(string emailAddress, string url)
    {
        LinkMails.Add((emailAddress, url));
        return Task.CompletedTask;
    }
}

public class FakeTrendService : ITrendService
{
    public TrendMeasurementEx? TrendResult { get; set; }

    public Task<TrendMeasurementEx?> GetTrendMeasurement(TimeSpan timeSpan, MeasurementLevelEx lastMeasurementLevelEx)
        => Task.FromResult(TrendResult);

    public Task<TrendMeasurementEx?[]> GetTrendMeasurements(MeasurementLevelEx lastMeasurementLevelEx, params TimeSpan[] fromHours)
        => Task.FromResult(fromHours.Select(_ => TrendResult).ToArray());
}

public static class TestEntityFactory
{
    public static Account CreateAccount(string? link = "test-link", string email = "test@example.com", bool isDemo = false)
    {
        var account = new Account
        {
            Uid = Guid.NewGuid(),
            Email = isDemo ? "demo@wateralarm.be" : email,
            CreationTimestamp = DateTime.UtcNow,
            Link = link
        };
        // Initialize the backing field so AccountSensors is not null
        var field = typeof(Account).GetField("_accountSensors", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        field.SetValue(account, new List<AccountSensor>());
        return account;
    }

    public static Sensor CreateSensor(string devEui = "test-sensor", SensorType type = SensorType.Level)
    {
        return new Sensor
        {
            Uid = Guid.NewGuid(),
            DevEui = devEui,
            CreateTimestamp = DateTime.UtcNow,
            Type = type,
            ExpectedIntervalSecs = 3600,
            Link = "test-sensor-link"
        };
    }

    public static AccountSensor CreateAccountSensor(
        Account? account = null,
        Sensor? sensor = null,
        int? distanceMmEmpty = 2000,
        int? distanceMmFull = 500,
        int? capacityL = 5000)
    {
        return new AccountSensor
        {
            Account = account ?? CreateAccount(),
            Sensor = sensor ?? CreateSensor(),
            CreateTimestamp = DateTime.UtcNow,
            DistanceMmEmpty = distanceMmEmpty,
            DistanceMmFull = distanceMmFull,
            CapacityL = capacityL
        };
    }

    public static MeasurementLevelEx CreateMeasurementLevelEx(AccountSensor? accountSensor = null, int distanceMm = 1000)
    {
        accountSensor ??= CreateAccountSensor();
        return new MeasurementLevelEx(
            new MeasurementLevel
            {
                DevEui = accountSensor.Sensor.DevEui,
                Timestamp = DateTime.UtcNow,
                BatV = 3.3,
                RssiDbm = -90,
                DistanceMm = distanceMm
            },
            accountSensor);
    }

    public static MeasurementDetectEx CreateMeasurementDetectEx(AccountSensor? accountSensor = null, int status = 1)
    {
        accountSensor ??= CreateAccountSensor(sensor: CreateSensor(type: SensorType.Detect));
        return new MeasurementDetectEx(
            new MeasurementDetect
            {
                DevEui = accountSensor.Sensor.DevEui,
                Timestamp = DateTime.UtcNow,
                BatV = 3.3,
                RssiDbm = -90,
                Status = status
            },
            accountSensor);
    }

    public static void SetupPageContext(PageModel model, DefaultHttpContext? httpContext = null)
    {
        httpContext ??= new DefaultHttpContext();
        var pageContext = new PageContext(new ActionContext(
            httpContext,
            new RouteData(),
            new Microsoft.AspNetCore.Mvc.RazorPages.PageActionDescriptor()));
        model.PageContext = pageContext;
    }
}
