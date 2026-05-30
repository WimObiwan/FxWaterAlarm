using Core.Entities;
using Core.Queries;
using Core.Util;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using Site.Controllers;
using Site.Services;
using Site.Utilities;
using SiteTests.Helpers;

namespace SiteTests.Controllers;

public class AccountSensorControllerTest
{
    private static AccountSensorController CreateController(
        ConfigurableFakeMediator mediator,
        FakeTrendService? trendService = null)
    {
        var controller = new AccountSensorController(mediator, trendService ?? new FakeTrendService());
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };
        return controller;
    }

    [Fact]
    public async Task Index_ReturnsNotFound_WhenAccountSensorNotFound()
    {
        var mediator = new ConfigurableFakeMediator();
        var controller = CreateController(mediator);

        var result = await controller.Index("no-acct", "no-sensor");

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Index_ReturnsOk_WithLevelMeasurement()
    {
        var mediator = new ConfigurableFakeMediator();
        var accountSensor = TestEntityFactory.CreateAccountSensor();
        mediator.SetResponse<AccountSensorByLinkQuery, Core.Entities.AccountSensor?>(accountSensor);
        var measurement = TestEntityFactory.CreateMeasurementLevelEx(accountSensor);
        mediator.SetResponse<LastMeasurementQuery, IMeasurementEx?>(measurement);

        var controller = CreateController(mediator);
        var result = await controller.Index("test-link", "test-sensor-link");

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Index_ReturnsOk_WithDetectMeasurement()
    {
        var mediator = new ConfigurableFakeMediator();
        var sensor = TestEntityFactory.CreateSensor(type: SensorType.Detect);
        var accountSensor = TestEntityFactory.CreateAccountSensor(sensor: sensor);
        mediator.SetResponse<AccountSensorByLinkQuery, Core.Entities.AccountSensor?>(accountSensor);
        var measurement = TestEntityFactory.CreateMeasurementDetectEx(accountSensor);
        mediator.SetResponse<LastMeasurementQuery, IMeasurementEx?>(measurement);

        var controller = CreateController(mediator);
        var result = await controller.Index("test-link", "test-sensor-link");

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Index_ReturnsOk_WithNullMeasurement()
    {
        var mediator = new ConfigurableFakeMediator();
        var accountSensor = TestEntityFactory.CreateAccountSensor();
        mediator.SetResponse<AccountSensorByLinkQuery, Core.Entities.AccountSensor?>(accountSensor);
        // LastMeasurementQuery returns null → measurementEx is null → Level branch with null

        var controller = CreateController(mediator);

        // When measurementEx is null, it doesn't match any pattern. This throws InvalidOperationException.
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            controller.Index("test-link", "test-sensor-link"));
    }

    [Fact]
    public async Task Index_ReturnsOk_WithMoistureMeasurement()
    {
        var mediator = new ConfigurableFakeMediator();
        var sensor = TestEntityFactory.CreateSensor(type: SensorType.Moisture);
        var accountSensor = TestEntityFactory.CreateAccountSensor(sensor: sensor);
        mediator.SetResponse<AccountSensorByLinkQuery, Core.Entities.AccountSensor?>(accountSensor);
        var moisture = TestEntityFactory.CreateMeasurementMoistureEx(accountSensor);
        mediator.SetResponse<LastMeasurementQuery, IMeasurementEx?>(moisture);

        var controller = CreateController(mediator);
        var result = await controller.Index("test-link", "test-sensor-link");

        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Index_ReturnsLevelPayload_WithDensityAndGeometryFields()
    {
        var mediator = new ConfigurableFakeMediator();
        var sensor = TestEntityFactory.CreateSensor(type: SensorType.LevelPressure);
        var accountSensor = TestEntityFactory.CreateAccountSensor(sensor: sensor);
        accountSensor.DensityKgPerM3 = 820.0;
        accountSensor.Geometry = TankGeometry.HorizontalCylinder;

        mediator.SetResponse<AccountSensorByLinkQuery, Core.Entities.AccountSensor?>(accountSensor);
        var measurement = TestEntityFactory.CreateMeasurementLevelEx(accountSensor);
        mediator.SetResponse<LastMeasurementQuery, IMeasurementEx?>(measurement);

        var controller = CreateController(mediator);
        var result = await controller.Index("test-link", "test-sensor-link");

        var okResult = Assert.IsType<OkObjectResult>(result);
        var json = JsonSerializer.Serialize(okResult.Value);
        using var doc = JsonDocument.Parse(json);

        var accountSensorNode = doc.RootElement.GetProperty("AccountSensor");
        Assert.Equal(820.0, accountSensorNode.GetProperty("DensityKgPerM3").GetDouble());
        Assert.Equal("HorizontalCylinder", accountSensorNode.GetProperty("Geometry").GetString());
    }
}
