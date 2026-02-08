using Core.Entities;
using Core.Queries;
using MediatR;
using Site.Pages;
using SiteTests.Helpers;
using static Site.Pages.AccountSensor;
using AccountSensorPage = Site.Pages.AccountSensor;

namespace SiteTests.Pages;

public class AccountSensorPageTest
{
    private static (AccountSensorPage model, ConfigurableFakeMediator mediator, FakeUserInfo userInfo) CreateModel()
    {
        var mediator = new ConfigurableFakeMediator();
        var userInfo = new FakeUserInfo { Authenticated = true, CanUpdate = true };
        var trendService = new FakeTrendService();
        var urlBuilder = new FakeUrlBuilder();
        var model = new AccountSensorPage(mediator, userInfo, trendService, urlBuilder);
        TestEntityFactory.SetupPageContext(model);
        return (model, mediator, userInfo);
    }

    private static Core.Entities.AccountSensor CreateAccountSensorEntity(SensorType type = SensorType.Level)
    {
        var sensor = TestEntityFactory.CreateSensor(type: type);
        var acctSensor = TestEntityFactory.CreateAccountSensor(sensor: sensor,
            distanceMmEmpty: 2000, distanceMmFull: 500, capacityL: 5000);
        return acctSensor;
    }

    // ----- UpdateSettings tests -----

    [Fact]
    public async Task UpdateSettings_ReturnsError_WhenPageIsNotSettings()
    {
        var (model, mediator, _) = CreateModel();
        var result = await model.UpdateSettings(mediator, "a", "s", PageTypeEnum.Diagram,
            "name", 0, 500, 2000, 0, 5000, true, null);
        Assert.Equal(SaveResultEnum.Error, result);
    }

    [Fact]
    public async Task UpdateSettings_ReturnsNotAuthorized_WhenNotAuthenticated()
    {
        var (model, mediator, userInfo) = CreateModel();
        userInfo.Authenticated = false;

        var result = await model.UpdateSettings(mediator, "a", "s", PageTypeEnum.Settings,
            "name", 0, 500, 2000, 0, 5000, true, null);
        Assert.Equal(SaveResultEnum.NotAuthorized, result);
    }

    [Fact]
    public async Task UpdateSettings_ReturnsInvalidData_WhenSensorNameIsNull()
    {
        var (model, mediator, _) = CreateModel();
        var result = await model.UpdateSettings(mediator, "a", "s", PageTypeEnum.Settings,
            null, 0, 500, 2000, 0, 5000, true, null);
        Assert.Equal(SaveResultEnum.InvalidData, result);
    }

    [Fact]
    public async Task UpdateSettings_ReturnsInvalidData_WhenCapacityNegative()
    {
        var (model, mediator, _) = CreateModel();
        var result = await model.UpdateSettings(mediator, "a", "s", PageTypeEnum.Settings,
            "name", 0, 500, 2000, 0, -1, true, null);
        Assert.Equal(SaveResultEnum.InvalidData, result);
    }

    [Fact]
    public async Task UpdateSettings_ReturnsInvalidData_WhenCapacityZero()
    {
        var (model, mediator, _) = CreateModel();
        var result = await model.UpdateSettings(mediator, "a", "s", PageTypeEnum.Settings,
            "name", 0, 500, 2000, 0, 0, true, null);
        Assert.Equal(SaveResultEnum.InvalidData, result);
    }

    [Fact]
    public async Task UpdateSettings_ReturnsInvalidData_WhenDistanceMmFullNegative()
    {
        var (model, mediator, _) = CreateModel();
        var result = await model.UpdateSettings(mediator, "a", "s", PageTypeEnum.Settings,
            "name", 0, -1, 2000, 0, 5000, true, null);
        Assert.Equal(SaveResultEnum.InvalidData, result);
    }

    [Fact]
    public async Task UpdateSettings_ReturnsInvalidData_WhenDistanceMmEmptyNegative()
    {
        var (model, mediator, _) = CreateModel();
        var result = await model.UpdateSettings(mediator, "a", "s", PageTypeEnum.Settings,
            "name", 0, 500, -1, 0, 5000, true, null);
        Assert.Equal(SaveResultEnum.InvalidData, result);
    }

    [Fact]
    public async Task UpdateSettings_ReturnsInvalidData_WhenUnusableHeightMmNegative()
    {
        var (model, mediator, _) = CreateModel();
        var result = await model.UpdateSettings(mediator, "a", "s", PageTypeEnum.Settings,
            "name", 0, 500, 2000, -1, 5000, true, null);
        Assert.Equal(SaveResultEnum.InvalidData, result);
    }

    [Fact]
    public async Task UpdateSettings_ReturnsError_WhenAccountSensorNotFound()
    {
        var (model, mediator, _) = CreateModel();
        // No response for AccountSensorByLinkQuery → returns null

        var result = await model.UpdateSettings(mediator, "a", "s", PageTypeEnum.Settings,
            "name", 0, 500, 2000, 0, 5000, true, null);
        Assert.Equal(SaveResultEnum.Error, result);
    }

    [Fact]
    public async Task UpdateSettings_ReturnsNotAuthorized_WhenCannotUpdate()
    {
        var (model, mediator, userInfo) = CreateModel();
        var acctSensor = CreateAccountSensorEntity();
        mediator.SetResponse<AccountSensorByLinkQuery, Core.Entities.AccountSensor?>(acctSensor);
        userInfo.CanUpdate = false;

        var result = await model.UpdateSettings(mediator, "a", "s", PageTypeEnum.Settings,
            "name", 0, 500, 2000, 0, 5000, true, null);
        Assert.Equal(SaveResultEnum.NotAuthorized, result);
    }

    [Fact]
    public async Task UpdateSettings_ReturnsSaved_WhenValid()
    {
        var (model, mediator, _) = CreateModel();
        var acctSensor = CreateAccountSensorEntity();
        mediator.SetResponse<AccountSensorByLinkQuery, Core.Entities.AccountSensor?>(acctSensor);

        var result = await model.UpdateSettings(mediator, "a", "s", PageTypeEnum.Settings,
            "My Sensor", 1, 500, 2000, 100, 5000, true, null);
        Assert.Equal(SaveResultEnum.Saved, result);
    }

    [Fact]
    public async Task UpdateSettings_ReturnsInvalidData_ForLevelSensor_WhenEmptyMinusUnusableLeThanFull()
    {
        var (model, mediator, _) = CreateModel();
        // Level sensor: distanceMmEmpty - unusableHeightMm must be > distanceMmFull
        var acctSensor = CreateAccountSensorEntity(SensorType.Level);
        mediator.SetResponse<AccountSensorByLinkQuery, Core.Entities.AccountSensor?>(acctSensor);

        // distanceMmEmpty=1000, unusableHeightMm=600 → 1000-600=400  <= distanceMmFull=500 → InvalidData
        var result = await model.UpdateSettings(mediator, "a", "s", PageTypeEnum.Settings,
            "name", 0, 500, 1000, 600, 5000, true, null);
        Assert.Equal(SaveResultEnum.InvalidData, result);
    }

    [Fact]
    public async Task UpdateSettings_ReturnsInvalidData_ForLevelPressureSensor_WhenUnusableGe()
    {
        var (model, mediator, _) = CreateModel();
        var acctSensor = CreateAccountSensorEntity(SensorType.LevelPressure);
        mediator.SetResponse<AccountSensorByLinkQuery, Core.Entities.AccountSensor?>(acctSensor);

        // LevelPressure: unusableHeightMm >= (distanceMmEmpty + distanceMmFull) → InvalidData
        var result = await model.UpdateSettings(mediator, "a", "s", PageTypeEnum.Settings,
            "name", 0, 500, 2000, 2500, 5000, true, null);
        Assert.Equal(SaveResultEnum.InvalidData, result);
    }

    [Fact]
    public async Task UpdateSettings_ReturnsInvalidData_WhenManholeAreaNegative()
    {
        var (model, mediator, _) = CreateModel();
        var acctSensor = CreateAccountSensorEntity();
        mediator.SetResponse<AccountSensorByLinkQuery, Core.Entities.AccountSensor?>(acctSensor);

        var result = await model.UpdateSettings(mediator, "a", "s", PageTypeEnum.Settings,
            "name", 0, 500, 2000, 0, 5000, true, "-1.5");
        Assert.Equal(SaveResultEnum.InvalidData, result);
    }

    [Fact]
    public async Task UpdateSettings_ReturnsInvalidData_WhenManholeAreaInvalid()
    {
        var (model, mediator, _) = CreateModel();
        var acctSensor = CreateAccountSensorEntity();
        mediator.SetResponse<AccountSensorByLinkQuery, Core.Entities.AccountSensor?>(acctSensor);

        var result = await model.UpdateSettings(mediator, "a", "s", PageTypeEnum.Settings,
            "name", 0, 500, 2000, 0, 5000, true, "abc");
        Assert.Equal(SaveResultEnum.InvalidData, result);
    }

    [Fact]
    public async Task UpdateSettings_AcceptsValidManholeArea()
    {
        var (model, mediator, _) = CreateModel();
        var acctSensor = CreateAccountSensorEntity();
        mediator.SetResponse<AccountSensorByLinkQuery, Core.Entities.AccountSensor?>(acctSensor);

        var result = await model.UpdateSettings(mediator, "a", "s", PageTypeEnum.Settings,
            "name", 0, 500, 2000, 0, 5000, true, "2.5");
        Assert.Equal(SaveResultEnum.Saved, result);
    }

    [Fact]
    public async Task UpdateSettings_AcceptsNullManholeArea()
    {
        var (model, mediator, _) = CreateModel();
        var acctSensor = CreateAccountSensorEntity();
        mediator.SetResponse<AccountSensorByLinkQuery, Core.Entities.AccountSensor?>(acctSensor);

        var result = await model.UpdateSettings(mediator, "a", "s", PageTypeEnum.Settings,
            "name", 0, 500, 2000, 0, 5000, true, null);
        Assert.Equal(SaveResultEnum.Saved, result);
    }

    [Fact]
    public async Task UpdateSettings_ReturnsError_WhenCommandThrows()
    {
        var (model, mediator, _) = CreateModel();
        var acctSensor = CreateAccountSensorEntity();
        mediator.SetResponse<AccountSensorByLinkQuery, Core.Entities.AccountSensor?>(acctSensor);
        // Make the mediator throw on the UpdateAccountSensorCommand
        // We need to configure the mediator to throw after the query succeeds
        // Since our FakeMediator throws on all sends when ThrowOnSend is set,
        // we'll use a custom approach: register a response and then set ThrowOnSend
        // after the query. This is tricky with the current FakeMediator.
        // Alternative: just verify the try/catch works by using a mediator that throws.

        var throwingMediator = new ThrowingFakeMediator(acctSensor);
        var result = await model.UpdateSettings(throwingMediator, "a", "s", PageTypeEnum.Settings,
            "name", 0, 500, 2000, 0, 5000, true, null);
        Assert.Equal(SaveResultEnum.Error, result);
    }

    [Fact]
    public async Task UpdateSettings_DefaultsOrderToZero_WhenNull()
    {
        var (model, mediator, _) = CreateModel();
        var acctSensor = CreateAccountSensorEntity();
        mediator.SetResponse<AccountSensorByLinkQuery, Core.Entities.AccountSensor?>(acctSensor);

        var result = await model.UpdateSettings(mediator, "a", "s", PageTypeEnum.Settings,
            "name", null, 500, 2000, 0, 5000, true, null);
        Assert.Equal(SaveResultEnum.Saved, result);
    }

    [Fact]
    public async Task UpdateSettings_DefaultsAlertsEnabled_WhenNull()
    {
        var (model, mediator, _) = CreateModel();
        var acctSensor = CreateAccountSensorEntity();
        mediator.SetResponse<AccountSensorByLinkQuery, Core.Entities.AccountSensor?>(acctSensor);

        var result = await model.UpdateSettings(mediator, "a", "s", PageTypeEnum.Settings,
            "name", 0, 500, 2000, 0, 5000, null, null);
        Assert.Equal(SaveResultEnum.Saved, result);
    }

    // --- OnGet tests ---

    [Fact]
    public async Task OnGet_LoadsAccountSensor()
    {
        var (model, mediator, _) = CreateModel();
        var acctSensor = CreateAccountSensorEntity();
        mediator.SetResponse<AccountSensorByLinkQuery, Core.Entities.AccountSensor?>(acctSensor);

        await model.OnGet("acct-link", "sensor-link");

        Assert.NotNull(model.AccountSensorEntity);
    }

    [Fact]
    public async Task OnGet_SetsPageType()
    {
        var (model, mediator, _) = CreateModel();
        var acctSensor = CreateAccountSensorEntity();
        mediator.SetResponse<AccountSensorByLinkQuery, Core.Entities.AccountSensor?>(acctSensor);

        await model.OnGet("a", "s", page: PageTypeEnum.Details);

        Assert.Equal(PageTypeEnum.Details, model.PageType);
    }

    [Fact]
    public async Task OnGet_AccountSensorNull_WhenNotFound()
    {
        var (model, _, _) = CreateModel();

        await model.OnGet("a", "s");

        Assert.Null(model.AccountSensorEntity);
    }

    /// <summary>
    /// Mediator that returns an AccountSensor for the query but throws on the command.
    /// </summary>
    private class ThrowingFakeMediator : IMediator
    {
        private readonly Core.Entities.AccountSensor? _accountSensor;

        public ThrowingFakeMediator(Core.Entities.AccountSensor? accountSensor)
        {
            _accountSensor = accountSensor;
        }

        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (request is AccountSensorByLinkQuery)
                return Task.FromResult((TResponse)(object)_accountSensor!);
            // For UpdateAccountSensorCommand (IRequest<Unit> → TResponse = MediatR.Unit or similar), throw
            throw new Exception("Command failed");
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
        {
            throw new Exception("Command failed");
        }

        public Task<object?> Send(object request, CancellationToken cancellationToken = default) => throw new Exception("Command failed");
        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) => AsyncEnumerable.Empty<TResponse>();
        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) => AsyncEnumerable.Empty<object?>();
        public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default) where TNotification : INotification => Task.CompletedTask;
    }
}
