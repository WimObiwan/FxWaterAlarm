using Core.Entities;
using Core.Util;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Options;
using Site.Services;
using Site.Utilities;
using Site.ViewComponents;
using SiteTests.Helpers;

namespace SiteTests.ViewComponents;

public class ViewComponentsTest
{
    private static ViewComponentContext CreateViewComponentContext()
    {
        var httpContext = new DefaultHttpContext();
        var viewContext = new ViewContext
        {
            HttpContext = httpContext,
            ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary())
        };
        return new ViewComponentContext { ViewContext = viewContext };
    }

    private static MeasurementDisplayOptions DefaultDisplayOptions() => new()
    {
        OldMeasurementThresholdIntervals = 3
    };

    // --- MeasurementDisplayModel ---

    [Fact]
    public void MeasurementDisplayModel_PropertiesAreSet()
    {
        var accountSensor = TestEntityFactory.CreateAccountSensor();
        var measurement = TestEntityFactory.CreateMeasurementLevelEx(accountSensor);

        var model = new MeasurementDisplayModel<MeasurementLevelEx>
        {
            Measurement = measurement,
            IsOldMeasurement = true
        };

        Assert.Same(measurement, model.Measurement);
        Assert.True(model.IsOldMeasurement);
    }

    // --- DiagramViewComponent ---

    [Fact]
    public async Task DiagramViewComponent_ReturnsViewWithModel()
    {
        var component = new DiagramViewComponent();
        component.ViewComponentContext = CreateViewComponentContext();

        var measurement = TestEntityFactory.CreateMeasurementLevelEx();
        var result = await component.InvokeAsync(measurement);

        var viewResult = Assert.IsType<ViewViewComponentResult>(result);
        var model = Assert.IsType<DiagramModel>(viewResult.ViewData!.Model);
        Assert.Same(measurement, model.MeasurementEx);
    }

    [Fact]
    public async Task DiagramViewComponent_AcceptsNullMeasurement()
    {
        var component = new DiagramViewComponent();
        component.ViewComponentContext = CreateViewComponentContext();

        var result = await component.InvokeAsync(null);

        var viewResult = Assert.IsType<ViewViewComponentResult>(result);
        var model = Assert.IsType<DiagramModel>(viewResult.ViewData!.Model);
        Assert.Null(model.MeasurementEx);
    }

    // --- TrendViewComponent ---

    [Fact]
    public async Task TrendViewComponent_ReturnsViewWithModel()
    {
        var component = new TrendViewComponent();
        component.ViewComponentContext = CreateViewComponentContext();

        var accountSensor = TestEntityFactory.CreateAccountSensor();

        var result = await component.InvokeAsync(accountSensor, null, null, null, null);

        var viewResult = Assert.IsType<ViewViewComponentResult>(result);
        var model = Assert.IsType<TrendModel>(viewResult.ViewData!.Model);
        Assert.Same(accountSensor, model.AccountSensorEntity);
        Assert.Null(model.TrendMeasurement6H);
    }

    // --- MeasurementsGraphViewComponent ---

    [Fact]
    public async Task MeasurementsGraphViewComponent_ReturnsViewWithModel()
    {
        var component = new MeasurementsGraphViewComponent();
        component.ViewComponentContext = CreateViewComponentContext();

        var accountSensor = TestEntityFactory.CreateAccountSensor();
        var measurements = Array.Empty<AggregatedMeasurementLevelEx>();

        var result = await component.InvokeAsync(accountSensor, measurements);

        var viewResult = Assert.IsType<ViewViewComponentResult>(result);
        var model = Assert.IsType<MeasurementsGraphModel>(viewResult.ViewData!.Model);
        Assert.Same(accountSensor, model.AccountSensorEntity);
        Assert.Same(measurements, model.Measurements);
    }

    // --- MeasurementsGraphNewViewComponent ---

    [Fact]
    public async Task MeasurementsGraphNewViewComponent_ReturnsViewWithModel()
    {
        var component = new MeasurementsGraphNewViewComponent();
        component.ViewComponentContext = CreateViewComponentContext();

        var accountSensor = TestEntityFactory.CreateAccountSensor();
        var result = await component.InvokeAsync(accountSensor, true, 7, GraphType.Height);

        var viewResult = Assert.IsType<ViewViewComponentResult>(result);
        var model = Assert.IsType<MeasurementsGraphNewModel>(viewResult.ViewData!.Model);
        Assert.Same(accountSensor, model.AccountSensorEntity);
        Assert.True(model.ShowTimelineSlider);
        Assert.Equal(7, model.FromDays);
        Assert.Equal(GraphType.Height, model.GraphType);
    }

    // --- WaterlevelViewComponent ---

    [Fact]
    public async Task WaterlevelViewComponent_ReturnsViewWithModel()
    {
        var component = new WaterlevelViewComponent(Options.Create(DefaultDisplayOptions()));
        component.ViewComponentContext = CreateViewComponentContext();

        var measurement = TestEntityFactory.CreateMeasurementLevelEx();
        var result = await component.InvokeAsync(measurement);

        var viewResult = Assert.IsType<ViewViewComponentResult>(result);
        var model = Assert.IsType<MeasurementDisplayModel<MeasurementLevelEx>>(viewResult.ViewData!.Model);
        Assert.Same(measurement, model.Measurement);
    }

    // --- WaterlevelNewViewComponent ---

    [Fact]
    public async Task WaterlevelNewViewComponent_ReturnsViewWithModel()
    {
        var component = new WaterlevelNewViewComponent(Options.Create(DefaultDisplayOptions()));
        component.ViewComponentContext = CreateViewComponentContext();

        var measurement = TestEntityFactory.CreateMeasurementLevelEx();
        var result = await component.InvokeAsync(measurement);

        var viewResult = Assert.IsType<ViewViewComponentResult>(result);
        var model = Assert.IsType<MeasurementDisplayModel<MeasurementLevelEx>>(viewResult.ViewData!.Model);
        Assert.Same(measurement, model.Measurement);
    }

    // --- DetectViewComponent ---

    [Fact]
    public async Task DetectViewComponent_ReturnsViewWithModel()
    {
        var component = new DetectViewComponent(Options.Create(DefaultDisplayOptions()));
        component.ViewComponentContext = CreateViewComponentContext();

        var measurement = TestEntityFactory.CreateMeasurementDetectEx();
        var result = await component.InvokeAsync(measurement);

        var viewResult = Assert.IsType<ViewViewComponentResult>(result);
        var model = Assert.IsType<MeasurementDisplayModel<MeasurementDetectEx>>(viewResult.ViewData!.Model);
        Assert.Same(measurement, model.Measurement);
    }

    // --- ThermometerViewComponent ---

    [Fact]
    public async Task ThermometerViewComponent_ReturnsViewWithModel()
    {
        var component = new ThermometerViewComponent(Options.Create(DefaultDisplayOptions()));
        component.ViewComponentContext = CreateViewComponentContext();

        var sensor = TestEntityFactory.CreateSensor(type: SensorType.Thermometer);
        var accountSensor = TestEntityFactory.CreateAccountSensor(sensor: sensor);
        var measurement = new MeasurementThermometerEx(
            new MeasurementThermometer
            {
                DevEui = "test",
                Timestamp = DateTime.UtcNow,
                BatV = 3.3,
                RssiDbm = -90,
                TempC = 22.5,
            },
            accountSensor);

        var result = await component.InvokeAsync(measurement);

        var viewResult = Assert.IsType<ViewViewComponentResult>(result);
        var model = Assert.IsType<MeasurementDisplayModel<MeasurementThermometerEx>>(viewResult.ViewData!.Model);
        Assert.Same(measurement, model.Measurement);
    }

    // --- MoistureViewComponent ---

    [Fact]
    public async Task MoistureViewComponent_ReturnsViewWithModel()
    {
        var component = new MoistureViewComponent(Options.Create(DefaultDisplayOptions()));
        component.ViewComponentContext = CreateViewComponentContext();

        var sensor = TestEntityFactory.CreateSensor(type: SensorType.Moisture);
        var accountSensor = TestEntityFactory.CreateAccountSensor(sensor: sensor);
        var measurement = new MeasurementMoistureEx(
            new MeasurementMoisture
            {
                DevEui = "test",
                Timestamp = DateTime.UtcNow,
                BatV = 3.3,
                RssiDbm = -90,
                SoilMoisturePrc = 45,
                SoilTemperature = 18,
                SoilConductivity = 120
            },
            accountSensor);

        var result = await component.InvokeAsync(measurement);

        var viewResult = Assert.IsType<ViewViewComponentResult>(result);
        var model = Assert.IsType<MeasurementDisplayModel<MeasurementMoistureEx>>(viewResult.ViewData!.Model);
        Assert.Same(measurement, model.Measurement);
    }

    // --- MeasurementDetailsViewComponent ---

    [Fact]
    public async Task MeasurementDetailsViewComponent_ReturnsViewWithModel()
    {
        var component = new MeasurementDetailsViewComponent();
        component.ViewComponentContext = CreateViewComponentContext();

        var accountSensor = TestEntityFactory.CreateAccountSensor();
        var measurement = TestEntityFactory.CreateMeasurementLevelEx(accountSensor);

        var result = await component.InvokeAsync(accountSensor, measurement);

        var viewResult = Assert.IsType<ViewViewComponentResult>(result);
        var model = Assert.IsType<MeasurementDetailsModel>(viewResult.ViewData!.Model);
        Assert.Same(measurement, model.MeasurementEx);
        Assert.Same(accountSensor, model.AccountSensor);
    }

    [Fact]
    public async Task MeasurementDetailsViewComponent_AcceptsNullMeasurement()
    {
        var component = new MeasurementDetailsViewComponent();
        component.ViewComponentContext = CreateViewComponentContext();

        var accountSensor = TestEntityFactory.CreateAccountSensor();

        var result = await component.InvokeAsync(accountSensor, null);

        var viewResult = Assert.IsType<ViewViewComponentResult>(result);
        var model = Assert.IsType<MeasurementDetailsModel>(viewResult.ViewData!.Model);
        Assert.Null(model.MeasurementEx);
    }

    // --- SensorSettingsViewComponent ---

    [Fact]
    public async Task SensorSettingsViewComponent_ReturnsViewWithModel()
    {
        var component = new SensorSettingsViewComponent();
        component.ViewComponentContext = CreateViewComponentContext();

        var accountSensor = TestEntityFactory.CreateAccountSensor();

        var result = await component.InvokeAsync(
            accountSensor, "/a/test-link/s/test-sensor-link",
            Site.Pages.AccountSensor.SaveResultEnum.None);

        var viewResult = Assert.IsType<ViewViewComponentResult>(result);
        var model = Assert.IsType<SensorSettingsModel>(viewResult.ViewData!.Model);
        Assert.Same(accountSensor, model.AccountSensor);
        Assert.Equal("/a/test-link/s/test-sensor-link", model.Url);
        Assert.Equal(Site.Pages.AccountSensor.SaveResultEnum.None, model.SaveResult);
    }

    [Fact]
    public void SensorSettingsModel_LoginUrl_BuildsFromAccountLink()
    {
        var account = TestEntityFactory.CreateAccount(link: "my-acct");
        var accountSensor = TestEntityFactory.CreateAccountSensor(account: account);
        var model = new SensorSettingsModel
        {
            AccountSensor = accountSensor,
            Url = "/a/my-acct/s/sensor1",
            SaveResult = Site.Pages.AccountSensor.SaveResultEnum.Saved
        };

        var loginUrl = model.LoginUrl;
        Assert.Contains("/Account/LoginMessage", loginUrl);
        Assert.Contains("a=my-acct", loginUrl);
    }
}
