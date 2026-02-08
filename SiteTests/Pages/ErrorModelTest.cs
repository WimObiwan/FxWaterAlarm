using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Site.Pages;
using SiteTests.Helpers;

namespace SiteTests.Pages;

public class ErrorModelTest
{
    [Fact]
    public void OnGet_SetsRequestId_FromActivity()
    {
        var model = new ErrorModel(NullLogger<ErrorModel>.Instance);
        var httpContext = new DefaultHttpContext();
        httpContext.TraceIdentifier = "trace-123";
        TestEntityFactory.SetupPageContext(model, httpContext);

        using var activity = new Activity("test").Start();
        model.OnGet();

        Assert.Equal(activity.Id, model.RequestId);
    }

    [Fact]
    public void OnGet_SetsRequestId_FromTraceIdentifier_WhenNoActivity()
    {
        var model = new ErrorModel(NullLogger<ErrorModel>.Instance);
        var httpContext = new DefaultHttpContext();
        httpContext.TraceIdentifier = "trace-456";
        TestEntityFactory.SetupPageContext(model, httpContext);

        // Ensure no current activity
        Activity.Current = null;
        model.OnGet();

        Assert.Equal("trace-456", model.RequestId);
    }

    [Fact]
    public void ShowRequestId_ReturnsTrue_WhenRequestIdIsSet()
    {
        var model = new ErrorModel(NullLogger<ErrorModel>.Instance);
        model.RequestId = "some-id";
        Assert.True(model.ShowRequestId);
    }

    [Fact]
    public void ShowRequestId_ReturnsFalse_WhenRequestIdIsNull()
    {
        var model = new ErrorModel(NullLogger<ErrorModel>.Instance);
        model.RequestId = null;
        Assert.False(model.ShowRequestId);
    }

    [Fact]
    public void ShowRequestId_ReturnsFalse_WhenRequestIdIsEmpty()
    {
        var model = new ErrorModel(NullLogger<ErrorModel>.Instance);
        model.RequestId = "";
        Assert.False(model.ShowRequestId);
    }
}
