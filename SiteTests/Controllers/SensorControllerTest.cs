using Microsoft.AspNetCore.Mvc;
using Site.Controllers;
using Xunit;

namespace SiteTests.Controllers;

public class SensorControllerTest
{
    [Fact]
    public void Index_ReturnsOkResult()
    {
        var controller = new SensorController();
        var result = controller.Index();

        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
    }
}
