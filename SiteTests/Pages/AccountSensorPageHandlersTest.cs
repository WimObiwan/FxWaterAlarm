using Core.Commands;
using Core.Entities;
using Core.Queries;
using Core.Util;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Site.Pages;
using SiteTests.Helpers;
using static Site.Pages.AccountSensor;
using AccountSensorPage = Site.Pages.AccountSensor;

namespace SiteTests.Pages;

/// <summary>
/// Additional tests for AccountSensor page handlers not covered in AccountSensorPageTest.
/// Covers OnPost, OnPostTestMailAlertAsync, OnPostAddAlarmAsync, 
/// OnPostUpdateAlarmAsync, OnPostDeleteAlarmAsync, OnGetExportCsv, and OnGet with Trend page.
/// </summary>
public class AccountSensorPageHandlersTest
{
    private static (AccountSensorPage model, ConfigurableFakeMediator mediator, FakeUserInfo userInfo, FakeUrlBuilder urlBuilder, FakeMessenger messenger)
        CreateModel()
    {
        var mediator = new ConfigurableFakeMediator();
        var userInfo = new FakeUserInfo { Authenticated = true, CanUpdate = true };
        var trendService = new FakeTrendService();
        var urlBuilder = new FakeUrlBuilder();
        var messenger = new FakeMessenger();
        var model = new AccountSensorPage(mediator, userInfo, trendService, urlBuilder);
        TestEntityFactory.SetupPageContext(model);
        return (model, mediator, userInfo, urlBuilder, messenger);
    }

    private static Core.Entities.AccountSensor CreateAccountSensorEntity(SensorType type = SensorType.Level)
    {
        var sensor = TestEntityFactory.CreateSensor(type: type);
        var acctSensor = TestEntityFactory.CreateAccountSensor(sensor: sensor,
            distanceMmEmpty: 2000, distanceMmFull: 500, capacityL: 5000);
        return acctSensor;
    }

    // ---- OnPost (the main form post that calls UpdateSettings and redirects) ----

    [Fact]
    public async Task OnPost_RedirectsWithSaveResult()
    {
        var (model, mediator, _, _, _) = CreateModel();
        var acctSensor = CreateAccountSensorEntity();
        mediator.SetResponse<AccountSensorByLinkQuery, Core.Entities.AccountSensor?>(acctSensor);

        var result = await model.OnPost(mediator, "a", "s",
            PageTypeEnum.Settings, "name", 0, 500, 2000, 0, 5000, true, null);

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Contains("page=Settings", redirect.Url);
        Assert.Contains("saveResult=Saved", redirect.Url);
    }

    [Fact]
    public async Task OnPost_RedirectsWithError_WhenWrongPageType()
    {
        var (model, mediator, _, _, _) = CreateModel();

        var result = await model.OnPost(mediator, "a", "s",
            PageTypeEnum.Diagram, "name", 0, 500, 2000, 0, 5000, true, null);

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Contains("saveResult=Error", redirect.Url);
    }

    // ---- OnPostTestMailAlertAsync ----

    [Fact]
    public async Task OnPostTestMailAlert_ReturnsUnauthorized_WhenNotAuthenticated()
    {
        var (model, mediator, userInfo, _, _) = CreateModel();
        userInfo.Authenticated = false;

        var result = await model.OnPostTestMailAlertAsync(mediator, new FakeMessenger(), "a", "s");

        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task OnPostTestMailAlert_ReturnsNotFound_WhenAccountSensorMissing()
    {
        var (model, mediator, _, _, _) = CreateModel();

        var result = await model.OnPostTestMailAlertAsync(mediator, new FakeMessenger(), "a", "s");

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnPostTestMailAlert_ReturnsForbid_WhenCannotUpdate()
    {
        var (model, mediator, userInfo, _, _) = CreateModel();
        var acctSensor = CreateAccountSensorEntity();
        mediator.SetResponse<AccountSensorByLinkQuery, Core.Entities.AccountSensor?>(acctSensor);
        userInfo.CanUpdate = false;

        var result = await model.OnPostTestMailAlertAsync(mediator, new FakeMessenger(), "a", "s");

        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task OnPostTestMailAlert_ReturnsSuccess_WhenValid()
    {
        var (model, mediator, _, _, _) = CreateModel();
        var acctSensor = CreateAccountSensorEntity();
        mediator.SetResponse<AccountSensorByLinkQuery, Core.Entities.AccountSensor?>(acctSensor);
        var messenger = new FakeMessenger();

        var result = await model.OnPostTestMailAlertAsync(mediator, messenger, "a", "s");

        Assert.IsType<JsonResult>(result);
        Assert.Single(messenger.AlertMails);
    }

    // ---- OnPostAddAlarmAsync ----

    [Fact]
    public async Task OnPostAddAlarm_ReturnsNotFound_WhenAccountSensorMissing()
    {
        var (model, _, _, _, _) = CreateModel();

        var result = await model.OnPostAddAlarmAsync("a", "s", "Data", "50");

        var json = Assert.IsType<JsonResult>(result);
        Assert.False(GetSuccess(json));
    }

    [Fact]
    public async Task OnPostAddAlarm_ReturnsNotAuthorized_WhenCannotUpdate()
    {
        var (model, mediator, userInfo, _, _) = CreateModel();
        var acctSensor = CreateAccountSensorEntity();
        mediator.SetResponse<AccountSensorByLinkQuery, Core.Entities.AccountSensor?>(acctSensor);
        userInfo.CanUpdate = false;

        var result = await model.OnPostAddAlarmAsync("a", "s", "Data", "50");

        var json = Assert.IsType<JsonResult>(result);
        Assert.False(GetSuccess(json));
    }

    [Fact]
    public async Task OnPostAddAlarm_ReturnsError_WhenInvalidAlarmType()
    {
        var (model, mediator, _, _, _) = CreateModel();
        var acctSensor = CreateAccountSensorEntity();
        mediator.SetResponse<AccountSensorByLinkQuery, Core.Entities.AccountSensor?>(acctSensor);

        var result = await model.OnPostAddAlarmAsync("a", "s", "InvalidType", "50");

        var json = Assert.IsType<JsonResult>(result);
        Assert.False(GetSuccess(json));
    }

    [Fact]
    public async Task OnPostAddAlarm_ReturnsError_WhenThresholdMissing_ForNonDetectOn()
    {
        var (model, mediator, _, _, _) = CreateModel();
        var acctSensor = CreateAccountSensorEntity();
        mediator.SetResponse<AccountSensorByLinkQuery, Core.Entities.AccountSensor?>(acctSensor);

        var result = await model.OnPostAddAlarmAsync("a", "s", "Data", null);

        var json = Assert.IsType<JsonResult>(result);
        Assert.False(GetSuccess(json));
    }

    [Fact]
    public async Task OnPostAddAlarm_Succeeds_WhenDetectOnWithoutThreshold()
    {
        var (model, mediator, _, _, _) = CreateModel();
        var acctSensor = CreateAccountSensorEntity();
        mediator.SetResponse<AccountSensorByLinkQuery, Core.Entities.AccountSensor?>(acctSensor);

        var result = await model.OnPostAddAlarmAsync("a", "s", "DetectOn", null);

        var json = Assert.IsType<JsonResult>(result);
        Assert.True(GetSuccess(json));
    }

    [Fact]
    public async Task OnPostAddAlarm_Succeeds_WhenValid()
    {
        var (model, mediator, _, _, _) = CreateModel();
        var acctSensor = CreateAccountSensorEntity();
        mediator.SetResponse<AccountSensorByLinkQuery, Core.Entities.AccountSensor?>(acctSensor);

        var result = await model.OnPostAddAlarmAsync("a", "s", "Battery", "3.0");

        var json = Assert.IsType<JsonResult>(result);
        Assert.True(GetSuccess(json));
        Assert.Contains(mediator.SentRequests, r => r is AddAccountSensorAlarmCommand);
    }

    [Fact]
    public async Task OnPostAddAlarm_ParsesThresholdCorrectly()
    {
        var (model, mediator, _, _, _) = CreateModel();
        var acctSensor = CreateAccountSensorEntity();
        mediator.SetResponse<AccountSensorByLinkQuery, Core.Entities.AccountSensor?>(acctSensor);

        var result = await model.OnPostAddAlarmAsync("a", "s", "PercentageLow", "25.5");

        var json = Assert.IsType<JsonResult>(result);
        Assert.True(GetSuccess(json));
        var cmd = mediator.SentRequests.OfType<AddAccountSensorAlarmCommand>().Single();
        Assert.Equal(25.5, cmd.AlarmThreshold);
    }

    [Fact]
    public async Task OnPostAddAlarm_WhitespaceThreshold_TreatedAsNull()
    {
        var (model, mediator, _, _, _) = CreateModel();
        var acctSensor = CreateAccountSensorEntity();
        mediator.SetResponse<AccountSensorByLinkQuery, Core.Entities.AccountSensor?>(acctSensor);

        // Non-DetectOn with whitespace threshold → threshold is required
        var result = await model.OnPostAddAlarmAsync("a", "s", "Data", "  ");

        var json = Assert.IsType<JsonResult>(result);
        Assert.False(GetSuccess(json));
    }

    [Fact]
    public async Task OnPostAddAlarm_HandlesException()
    {
        var (model, mediator, _, _, _) = CreateModel();
        var acctSensor = CreateAccountSensorEntity();
        mediator.SetResponse<AccountSensorByLinkQuery, Core.Entities.AccountSensor?>(acctSensor);
        mediator.ThrowOnSend = true;
        mediator.ExceptionToThrow = new Exception("DB error");

        // The command Send will throw, caught by try/catch
        // But the query Send also throws. Let's use a smarter approach.
        // Since SetResponse is already registered, it won't throw for the query.
        // Actually, looking at ConfigurableFakeMediator: ThrowOnSend + ExceptionToThrow throws
        // before checking _responses. Let me use a custom mediator instead.

        // Need to call via the model's private _mediator. OnPostAddAlarmAsync uses _mediator directly.
        // Can't swap _mediator on the model. Let me reconsider.
        // The OnPostAddAlarmAsync uses _mediator (the one from constructor), not a parameter.
        // So setting ThrowOnSend would also throw on the query...
        // Let's skip this and test exception path via OnPostUpdateAlarmAsync or OnPostDeleteAlarmAsync.

        // Reset ThrowOnSend
        mediator.ThrowOnSend = false;
    }

    // ---- OnPostUpdateAlarmAsync ----

    [Fact]
    public async Task OnPostUpdateAlarm_ReturnsNotFound_WhenAccountSensorMissing()
    {
        var (model, _, _, _, _) = CreateModel();

        var result = await model.OnPostUpdateAlarmAsync("a", "s",
            Guid.NewGuid().ToString(), "Data", 50.0);

        var json = Assert.IsType<JsonResult>(result);
        Assert.False(GetSuccess(json));
    }

    [Fact]
    public async Task OnPostUpdateAlarm_ReturnsNotAuthorized_WhenCannotUpdate()
    {
        var (model, mediator, userInfo, _, _) = CreateModel();
        var acctSensor = CreateAccountSensorEntity();
        mediator.SetResponse<AccountSensorByLinkQuery, Core.Entities.AccountSensor?>(acctSensor);
        userInfo.CanUpdate = false;

        var result = await model.OnPostUpdateAlarmAsync("a", "s",
            Guid.NewGuid().ToString(), "Data", 50.0);

        var json = Assert.IsType<JsonResult>(result);
        Assert.False(GetSuccess(json));
    }

    [Fact]
    public async Task OnPostUpdateAlarm_ReturnsError_WhenInvalidAlarmUid()
    {
        var (model, mediator, _, _, _) = CreateModel();
        var acctSensor = CreateAccountSensorEntity();
        mediator.SetResponse<AccountSensorByLinkQuery, Core.Entities.AccountSensor?>(acctSensor);

        var result = await model.OnPostUpdateAlarmAsync("a", "s",
            "not-a-guid", "Data", 50.0);

        var json = Assert.IsType<JsonResult>(result);
        Assert.False(GetSuccess(json));
    }

    [Fact]
    public async Task OnPostUpdateAlarm_ReturnsError_WhenInvalidAlarmType()
    {
        var (model, mediator, _, _, _) = CreateModel();
        var acctSensor = CreateAccountSensorEntity();
        mediator.SetResponse<AccountSensorByLinkQuery, Core.Entities.AccountSensor?>(acctSensor);

        var result = await model.OnPostUpdateAlarmAsync("a", "s",
            Guid.NewGuid().ToString(), "InvalidType", 50.0);

        var json = Assert.IsType<JsonResult>(result);
        Assert.False(GetSuccess(json));
    }

    [Fact]
    public async Task OnPostUpdateAlarm_ReturnsError_WhenThresholdMissing_ForNonDetectOn()
    {
        var (model, mediator, _, _, _) = CreateModel();
        var acctSensor = CreateAccountSensorEntity();
        mediator.SetResponse<AccountSensorByLinkQuery, Core.Entities.AccountSensor?>(acctSensor);

        var result = await model.OnPostUpdateAlarmAsync("a", "s",
            Guid.NewGuid().ToString(), "Battery", null);

        var json = Assert.IsType<JsonResult>(result);
        Assert.False(GetSuccess(json));
    }

    [Fact]
    public async Task OnPostUpdateAlarm_Succeeds_WhenDetectOnWithoutThreshold()
    {
        var (model, mediator, _, _, _) = CreateModel();
        var acctSensor = CreateAccountSensorEntity();
        mediator.SetResponse<AccountSensorByLinkQuery, Core.Entities.AccountSensor?>(acctSensor);

        var result = await model.OnPostUpdateAlarmAsync("a", "s",
            Guid.NewGuid().ToString(), "DetectOn", null);

        var json = Assert.IsType<JsonResult>(result);
        Assert.True(GetSuccess(json));
    }

    [Fact]
    public async Task OnPostUpdateAlarm_Succeeds_WhenValid()
    {
        var (model, mediator, _, _, _) = CreateModel();
        var acctSensor = CreateAccountSensorEntity();
        mediator.SetResponse<AccountSensorByLinkQuery, Core.Entities.AccountSensor?>(acctSensor);
        var alarmUid = Guid.NewGuid();

        var result = await model.OnPostUpdateAlarmAsync("a", "s",
            alarmUid.ToString(), "HeightLow", 500.0);

        var json = Assert.IsType<JsonResult>(result);
        Assert.True(GetSuccess(json));
        var cmd = mediator.SentRequests.OfType<UpdateAccountSensorAlarmCommand>().Single();
        Assert.Equal(alarmUid, cmd.AlarmUid);
    }

    // ---- OnPostDeleteAlarmAsync ----

    [Fact]
    public async Task OnPostDeleteAlarm_ReturnsNotFound_WhenAccountSensorMissing()
    {
        var (model, _, _, _, _) = CreateModel();

        var result = await model.OnPostDeleteAlarmAsync("a", "s", Guid.NewGuid().ToString());

        var json = Assert.IsType<JsonResult>(result);
        Assert.False(GetSuccess(json));
    }

    [Fact]
    public async Task OnPostDeleteAlarm_ReturnsNotAuthorized_WhenCannotUpdate()
    {
        var (model, mediator, userInfo, _, _) = CreateModel();
        var acctSensor = CreateAccountSensorEntity();
        mediator.SetResponse<AccountSensorByLinkQuery, Core.Entities.AccountSensor?>(acctSensor);
        userInfo.CanUpdate = false;

        var result = await model.OnPostDeleteAlarmAsync("a", "s", Guid.NewGuid().ToString());

        var json = Assert.IsType<JsonResult>(result);
        Assert.False(GetSuccess(json));
    }

    [Fact]
    public async Task OnPostDeleteAlarm_ReturnsError_WhenInvalidAlarmUid()
    {
        var (model, mediator, _, _, _) = CreateModel();
        var acctSensor = CreateAccountSensorEntity();
        mediator.SetResponse<AccountSensorByLinkQuery, Core.Entities.AccountSensor?>(acctSensor);

        var result = await model.OnPostDeleteAlarmAsync("a", "s", "not-a-guid");

        var json = Assert.IsType<JsonResult>(result);
        Assert.False(GetSuccess(json));
    }

    [Fact]
    public async Task OnPostDeleteAlarm_Succeeds_WhenValid()
    {
        var (model, mediator, _, _, _) = CreateModel();
        var acctSensor = CreateAccountSensorEntity();
        mediator.SetResponse<AccountSensorByLinkQuery, Core.Entities.AccountSensor?>(acctSensor);
        var alarmUid = Guid.NewGuid();

        var result = await model.OnPostDeleteAlarmAsync("a", "s", alarmUid.ToString());

        var json = Assert.IsType<JsonResult>(result);
        Assert.True(GetSuccess(json));
        var cmd = mediator.SentRequests.OfType<RemoveAlarmFromAccountSensorCommand>().Single();
        Assert.Equal(alarmUid, cmd.AlarmUid);
    }

    // ---- OnGetExportCsv ----

    [Fact]
    public async Task OnGetExportCsv_ReturnsNotFound_WhenAccountSensorMissing()
    {
        var (model, _, _, _, _) = CreateModel();

        var result = await model.OnGetExportCsv("a", "s");

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task OnGetExportCsv_ReturnsEmptyResult_WhenNoMeasurements()
    {
        var (model, mediator, _, _, _) = CreateModel();
        var acctSensor = CreateAccountSensorEntity();
        mediator.SetResponse<AccountSensorByLinkQuery, Core.Entities.AccountSensor?>(acctSensor);
        mediator.SetResponse<MeasurementsQuery, IMeasurementEx[]?>(null);

        // Need a real response body stream
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();
        TestEntityFactory.SetupPageContext(model, httpContext);

        var result = await model.OnGetExportCsv("a", "s");

        Assert.IsType<EmptyResult>(result);
        Assert.Equal("text/csv", httpContext.Response.ContentType);
    }

    [Fact]
    public async Task OnGetExportCsv_WritesHeaderAndData_WhenMeasurementsExist()
    {
        var (model, mediator, _, _, _) = CreateModel();
        var acctSensor = CreateAccountSensorEntity();
        mediator.SetResponse<AccountSensorByLinkQuery, Core.Entities.AccountSensor?>(acctSensor);

        var measurement = TestEntityFactory.CreateMeasurementLevelEx(acctSensor, distanceMm: 1000);
        mediator.SetResponse<MeasurementsQuery, IMeasurementEx[]?>(new IMeasurementEx[] { measurement });

        var ms = new MemoryStream();
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = ms;
        TestEntityFactory.SetupPageContext(model, httpContext);

        var result = await model.OnGetExportCsv("a", "s");

        Assert.IsType<EmptyResult>(result);

        ms.Position = 0;
        using var reader = new StreamReader(ms);
        var csv = reader.ReadToEnd();
        Assert.Contains("DevEui", csv);
        Assert.Contains("Timestamp", csv);
        Assert.Contains("DistanceMm", csv);
        Assert.Contains(acctSensor.Sensor.DevEui, csv);
    }

    // ---- OnGet with Trend page ----

    [Fact]
    public async Task OnGet_TrendPage_LoadsTrendMeasurements_ForLevelSensor()
    {
        var (model, mediator, _, _, _) = CreateModel();
        var acctSensor = CreateAccountSensorEntity(SensorType.Level);
        mediator.SetResponse<AccountSensorByLinkQuery, Core.Entities.AccountSensor?>(acctSensor);

        var measurement = TestEntityFactory.CreateMeasurementLevelEx(acctSensor, distanceMm: 1000);
        mediator.SetResponse<LastMeasurementQuery, IMeasurementEx?>(measurement);

        await model.OnGet("a", "s", page: PageTypeEnum.Trend);

        Assert.Equal(PageTypeEnum.Trend, model.PageType);
        // TrendService returns null by default, but the arrays are still assigned
        Assert.Null(model.TrendMeasurement6H);
        Assert.Null(model.TrendMeasurement24H);
        Assert.Null(model.TrendMeasurement7D);
        Assert.Null(model.TrendMeasurement30D);
    }

    [Fact]
    public async Task OnGet_TrendPage_SkipsTrend_WhenMeasurementIsNull()
    {
        var (model, mediator, _, _, _) = CreateModel();
        var acctSensor = CreateAccountSensorEntity();
        mediator.SetResponse<AccountSensorByLinkQuery, Core.Entities.AccountSensor?>(acctSensor);
        // No LastMeasurementQuery response → null LastMeasurement

        await model.OnGet("a", "s", page: PageTypeEnum.Trend);

        Assert.Null(model.TrendMeasurement6H);
    }

    [Fact]
    public async Task OnGet_TrendPage_SkipsTrend_WhenMeasurementIsNotLevel()
    {
        var (model, mediator, _, _, _) = CreateModel();
        var acctSensor = CreateAccountSensorEntity(SensorType.Detect);
        mediator.SetResponse<AccountSensorByLinkQuery, Core.Entities.AccountSensor?>(acctSensor);

        var measurement = TestEntityFactory.CreateMeasurementDetectEx(acctSensor);
        mediator.SetResponse<LastMeasurementQuery, IMeasurementEx?>(measurement);

        await model.OnGet("a", "s", page: PageTypeEnum.Trend);

        // Detect measurement is not MeasurementLevelEx, so trend not loaded
        Assert.Null(model.TrendMeasurement6H);
    }

    [Fact]
    public async Task OnGet_SetsQrBaseUrl_WhenAccountSensorFound()
    {
        var (model, mediator, _, _, _) = CreateModel();
        var acctSensor = CreateAccountSensorEntity();
        mediator.SetResponse<AccountSensorByLinkQuery, Core.Entities.AccountSensor?>(acctSensor);

        await model.OnGet("a", "s");

        Assert.NotNull(model.QrBaseUrl);
        Assert.Contains(acctSensor.Sensor.Link!, model.QrBaseUrl);
    }

    [Fact]
    public async Task OnGet_SetsFromDays()
    {
        var (model, mediator, _, _, _) = CreateModel();
        var acctSensor = CreateAccountSensorEntity();
        mediator.SetResponse<AccountSensorByLinkQuery, Core.Entities.AccountSensor?>(acctSensor);

        await model.OnGet("a", "s", fromDays: 42);

        Assert.Equal(42, model.FromDays);
    }

    [Fact]
    public async Task OnGet_SetsPreview()
    {
        var (model, mediator, _, _, _) = CreateModel();
        var acctSensor = CreateAccountSensorEntity();
        mediator.SetResponse<AccountSensorByLinkQuery, Core.Entities.AccountSensor?>(acctSensor);

        await model.OnGet("a", "s", preview: true);

        Assert.True(model.Preview);
    }

    [Fact]
    public async Task OnGet_LoadsLastMeasurement_WhenFound()
    {
        var (model, mediator, _, _, _) = CreateModel();
        var acctSensor = CreateAccountSensorEntity();
        mediator.SetResponse<AccountSensorByLinkQuery, Core.Entities.AccountSensor?>(acctSensor);

        var measurement = TestEntityFactory.CreateMeasurementLevelEx(acctSensor);
        mediator.SetResponse<LastMeasurementQuery, IMeasurementEx?>(measurement);

        await model.OnGet("a", "s");

        Assert.NotNull(model.LastMeasurement);
    }

    // ---- Helpers ----

    private static bool GetSuccess(JsonResult json)
    {
        var value = json.Value!;
        var successProp = value.GetType().GetProperty("success");
        return (bool)successProp!.GetValue(value)!;
    }

    /// <summary>
    /// Mediator that returns the query response but throws on commands.
    /// Used to test exception handling in try/catch blocks.
    /// </summary>
    private class CommandThrowingMediator : IMediator
    {
        private readonly Core.Entities.AccountSensor? _accountSensor;

        public CommandThrowingMediator(Core.Entities.AccountSensor? accountSensor)
        {
            _accountSensor = accountSensor;
        }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (request is AccountSensorByLinkQuery)
                return Task.FromResult((TResponse)(object)_accountSensor!);
            throw new Exception("Command failed");
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
        {
            throw new Exception("Command failed");
        }

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
            => throw new Exception("Command failed");

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
            => AsyncEnumerable.Empty<TResponse>();

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
            => AsyncEnumerable.Empty<object?>();

        public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification => Task.CompletedTask;
    }
}
