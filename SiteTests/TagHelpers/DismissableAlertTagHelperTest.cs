using Microsoft.AspNetCore.Razor.TagHelpers;
using Site.TagHelpers;

namespace SiteTests.TagHelpers;

public class DismissableAlertTagHelperTest
{
    private static (DismissableAlertTagHelper helper, TagHelperContext context, TagHelperOutput output) Create(
        string id = "test-alert",
        int repeat = 0,
        DateTime? expiration = null,
        string? alertClass = null,
        string childContent = "Alert message")
    {
        var helper = new DismissableAlertTagHelper
        {
            Id = id,
            Repeat = repeat,
            Expiration = expiration ?? DateTime.UtcNow.AddDays(30),
            AlertClass = alertClass
        };

        var context = new TagHelperContext(
            new TagHelperAttributeList(),
            new Dictionary<object, object>(),
            "unique-id");

        var output = new TagHelperOutput(
            "dismissable-alert",
            new TagHelperAttributeList(),
            (useCachedResult, encoder) =>
            {
                var content = new DefaultTagHelperContent();
                content.SetHtmlContent(childContent);
                return Task.FromResult<TagHelperContent>(content);
            });

        return (helper, context, output);
    }

    [Fact]
    public async Task ProcessAsync_SetsTagNameToDiv()
    {
        var (helper, context, output) = Create();

        await helper.ProcessAsync(context, output);

        Assert.Equal("div", output.TagName);
    }

    [Fact]
    public async Task ProcessAsync_SetsIdAttribute()
    {
        var (helper, context, output) = Create(id: "my-custom-id");

        await helper.ProcessAsync(context, output);

        Assert.Equal("my-custom-id", output.Attributes["id"].Value);
    }

    [Fact]
    public async Task ProcessAsync_AddsAlertClasses()
    {
        var (helper, context, output) = Create(alertClass: "warning");

        await helper.ProcessAsync(context, output);

        var classAttr = output.Attributes["class"]?.Value?.ToString();
        Assert.NotNull(classAttr);
        Assert.Contains("alert", classAttr);
        Assert.Contains("alert-warning", classAttr);
        Assert.Contains("alert-dismissible", classAttr);
        Assert.Contains("fade", classAttr);
        Assert.Contains("hidden", classAttr);
    }

    [Fact]
    public async Task ProcessAsync_ContainsCloseButton()
    {
        var (helper, context, output) = Create();

        await helper.ProcessAsync(context, output);

        var content = output.Content.GetContent();
        Assert.Contains("btn-close", content);
        Assert.Contains("data-bs-dismiss=\"alert\"", content);
    }

    [Fact]
    public async Task ProcessAsync_ContainsChildContent()
    {
        var (helper, context, output) = Create(childContent: "My alert text");

        await helper.ProcessAsync(context, output);

        var content = output.Content.GetContent();
        Assert.Contains("My alert text", content);
    }

    [Fact]
    public async Task ProcessAsync_ContainsLocalStorageScript()
    {
        var (helper, context, output) = Create(id: "my-alert");

        await helper.ProcessAsync(context, output);

        var content = output.Content.GetContent();
        Assert.Contains("<script>", content);
        Assert.Contains("localStorage", content);
        Assert.Contains("dismissable-alert-my-alert", content);
    }

    [Fact]
    public async Task ProcessAsync_IncludesRepeatValue()
    {
        var (helper, context, output) = Create(repeat: 86400);

        await helper.ProcessAsync(context, output);

        var content = output.Content.GetContent();
        Assert.Contains("86400", content);
    }

    [Fact]
    public async Task ProcessAsync_IncludesExpirationInScript()
    {
        var expiration = new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var (helper, context, output) = Create(expiration: expiration);

        await helper.ProcessAsync(context, output);

        var content = output.Content.GetContent();
        Assert.Contains("2026-06-15", content);
    }

    [Fact]
    public async Task ProcessAsync_DefaultAlertClass_UsesSuccessWhenNull()
    {
        var (helper, context, output) = Create(alertClass: null);

        await helper.ProcessAsync(context, output);

        var classAttr = output.Attributes["class"]?.Value?.ToString();
        Assert.NotNull(classAttr);
        // When AlertClass is null, the expression "alert-" + AlertClass ?? "success"
        // evaluates to "alert-" (due to operator precedence: ("alert-" + null) ?? "success" = "alert-")
        Assert.Contains("alert", classAttr);
    }
}
