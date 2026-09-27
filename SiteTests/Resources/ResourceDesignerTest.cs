using Xunit;

namespace SiteTests.Resources;

/// <summary>
/// Tests that resource Designer classes are correctly generated,
/// their ResourceManager is initialized, and string properties return non-null values.
/// Uses InternalsVisibleTo to access the internal generated classes.
/// </summary>
public class ResourceDesignerTest
{
    // --- Pages/Account ---

    [Fact]
    public void Account_ResourceManager_IsNotNull()
    {
        Assert.NotNull(Site.Resources.Pages.Account.ResourceManager);
    }

    [Fact]
    public void Account_YourAccount_ReturnsValue()
    {
        Assert.NotNull(Site.Resources.Pages.Account.Your_account);
    }

    [Fact]
    public void Account_SensorDetails_ReturnsValue()
    {
        Assert.NotNull(Site.Resources.Pages.Account.Sensor_details);
    }

    // --- Pages/AccountLoginMessage ---

    [Fact]
    public void AccountLoginMessage_ResourceManager_IsNotNull()
    {
        Assert.NotNull(Site.Resources.Pages.AccountLoginMessage.ResourceManager);
    }

    [Fact]
    public void AccountLoginMessage_LogIn_ReturnsValue()
    {
        Assert.NotNull(Site.Resources.Pages.AccountLoginMessage.Log_in);
    }

    [Fact]
    public void AccountLoginMessage_Code_ReturnsValue()
    {
        Assert.NotNull(Site.Resources.Pages.AccountLoginMessage.code);
    }

    // --- Pages/AccountSensor ---

    [Fact]
    public void AccountSensor_ResourceManager_IsNotNull()
    {
        Assert.NotNull(Site.Resources.Pages.AccountSensor.ResourceManager);
    }

    [Fact]
    public void AccountSensor_Graph_ReturnsValue()
    {
        Assert.NotNull(Site.Resources.Pages.AccountSensor.Graph);
    }

    [Fact]
    public void AccountSensor_Details_ReturnsValue()
    {
        Assert.NotNull(Site.Resources.Pages.AccountSensor.Details);
    }

    [Fact]
    public void AccountSensor_Percentage_ReturnsValue()
    {
        Assert.NotNull(Site.Resources.Pages.AccountSensor.Percentage);
    }

    [Fact]
    public void AccountSensor_Height_ReturnsValue()
    {
        Assert.NotNull(Site.Resources.Pages.AccountSensor.Height);
    }

    // --- Pages/Auto ---

    [Fact]
    public void Auto_ResourceManager_IsNotNull()
    {
        Assert.NotNull(Site.Resources.Pages.Auto.ResourceManager);
    }

    [Fact]
    public void Auto_SetLink_ReturnsValue()
    {
        Assert.NotNull(Site.Resources.Pages.Auto.Set_link);
    }

    // --- Views/Shared/_Layout ---

    [Fact]
    public void Layout_ResourceManager_IsNotNull()
    {
        Assert.NotNull(Site.Resources.Views.Shared._Layout.ResourceManager);
    }

    [Fact]
    public void Layout_Language_ReturnsValue()
    {
        Assert.NotNull(Site.Resources.Views.Shared._Layout.Language);
    }

    [Fact]
    public void Layout_Documentation_ReturnsValue()
    {
        Assert.NotNull(Site.Resources.Views.Shared._Layout.Documentation);
    }

    [Fact]
    public void Layout_SetLink_ReturnsValue()
    {
        Assert.NotNull(Site.Resources.Views.Shared._Layout.Set_link);
    }
}
