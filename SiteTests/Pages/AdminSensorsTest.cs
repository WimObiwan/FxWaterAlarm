using Core.Commands;
using Core.Entities;
using Core.Queries;
using Microsoft.AspNetCore.Mvc;
using Site.Pages;
using SiteTests.Helpers;

namespace SiteTests.Pages;

public class AdminSensorsTest
{
    private static (AdminSensors model, ConfigurableFakeMediator mediator) CreateModel()
    {
        var mediator = new ConfigurableFakeMediator();
        var model = new AdminSensors(mediator);
        TestEntityFactory.SetupPageContext(model);
        return (model, mediator);
    }

    [Fact]
    public async Task OnGet_SetsMessage()
    {
        var (model, _) = CreateModel();

        await model.OnGet("test message", null);

        Assert.Equal("test message", model.Message);
    }

    [Fact]
    public async Task OnGet_LoadsSensor_WhenUidProvided()
    {
        var (model, mediator) = CreateModel();
        var sensor = TestEntityFactory.CreateSensor();
        mediator.SetResponse<SensorQuery, Core.Entities.Sensor?>(sensor);
        var uid = Guid.NewGuid();

        await model.OnGet(null, uid);

        Assert.NotNull(model.SensorEntity);
    }

    [Fact]
    public async Task OnGet_SensorIsNull_WhenNoUidProvided()
    {
        var (model, _) = CreateModel();

        await model.OnGet(null, null);

        Assert.Null(model.SensorEntity);
    }

    [Fact]
    public async Task OnPostAddSensor_CreatesSensorAndRedirects()
    {
        var (model, mediator) = CreateModel();
        var sensor = TestEntityFactory.CreateSensor();
        mediator.SetResponse<SensorQuery, Core.Entities.Sensor?>(sensor);

        var result = await model.OnPostAddSensor("AA:BB:CC:DD", SensorType.Level);

        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("Sensor created successfully.", redirect.RouteValues?["message"]);
        Assert.Contains(mediator.SentRequests, r => r is CreateSensorCommand);
        Assert.Contains(mediator.SentRequests, r => r is RegenerateSensorLinkCommand);
    }

    [Fact]
    public async Task OnPostAddSensor_ReturnsNotFound_WhenSensorNotCreated()
    {
        var (model, _) = CreateModel();
        // SensorQuery returns null

        var result = await model.OnPostAddSensor("AA:BB:CC:DD", SensorType.Level);

        Assert.IsType<NotFoundResult>(result);
    }
}
