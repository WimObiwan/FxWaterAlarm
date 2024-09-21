using System.Security.Cryptography;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Site.TagHelpers;

[HtmlTargetElement("dismissable-alert")]  
public class DismissableAlertTagHelper : TagHelper
{
    public required string Id { get; set; }
    public int Expiration { get; set; }
    public string? AlertClass { get; set; }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        string localStorageName = $"dismissable-alert-{Id}";
        output.TagName = "div";
        output.Attributes.SetAttribute("id", Id);
        output.AddClass("alert", HtmlEncoder.Default);
        output.AddClass("alert-" + AlertClass ?? "success", HtmlEncoder.Default);
        output.AddClass("alert-dismissible", HtmlEncoder.Default);
        output.AddClass("fade", HtmlEncoder.Default);
        output.AddClass("in", HtmlEncoder.Default);
        output.AddClass("hidden", HtmlEncoder.Default);

        output.Content.AppendHtml("""
<button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
""");

        output.Content.AppendHtml(await output.GetChildContentAsync());
        output.Content.AppendHtml($$"""
<script>
    window.addEventListener('load', function () {
    	const now = new Date()
        const store = localStorage.getItem("{{localStorageName}}")
        if (store === null || Date.parse(store) < now.getTime() - {{Expiration}} * 1000) {
            $("#{{Id}}").removeClass("hidden");
            $("#{{Id}}").addClass("show");
        }
        $("#{{Id}} .btn-close").on("click",function() {
            localStorage.setItem("{{localStorageName}}", now.toISOString());
        });
    })
</script>
""");
    }
}