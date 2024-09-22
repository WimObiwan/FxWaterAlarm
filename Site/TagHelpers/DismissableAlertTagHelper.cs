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
    public int Repeat { get; set; }
    public DateTime Expiration { get; set; }
    public string? AlertClass { get; set; }

    public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
    {
        const string localStoragePrefix = "dismissable-alert-";
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
    	const now = new Date();
        show = false;
        try {
            const store = JSON.parse(localStorage.getItem("{{localStorageName}}"));
            if (store === null) {
                show = true;
            } else {
                const expiration = store.expiration;
                if (expiration === null || Date.parse(expiration) < now.getTime()) {
                    show = true;
                } else {
                    const dismissed = store.dismissed;
                    if (dismissed === null || Date.parse(dismissed) < now.getTime() - {{Repeat}} * 1000) {
                        show = true;
                    }
                }
            }
        } catch (e) {
            show = true;
        }
        if (show === true) {
            $("#{{Id}}").removeClass("hidden");
            $("#{{Id}}").addClass("show");
        }
        $("#{{Id}} .btn-close").on("click",function() {
            $.each(localStorage, function(key, value) {
                if (key.startsWith("{{localStoragePrefix}}")) {
                    show = false;
                    try {
                        const store = JSON.parse(value);
                        if (store === null) {
                            show = true;
                        } else {
                            const expiration = store.expiration;
                            if (expiration === null || Date.parse(expiration) < now.getTime()) {
                                show = true;
                            } else {
                                const dismissed = store.dismissed;
                                if (dismissed === null || Date.parse(dismissed) < now.getTime() - {{Repeat}} * 1000) {
                                    show = true;
                                }
                            }
                        }
                    } catch (e) {
                        show = true;
                    }
                    if (show === true) {
                        localStorage.removeItem(key);
                    }
                }
                console.log(key, value);
            });
            var store = {"dismissed": now.toISOString(), "expiration": "{{Expiration.ToString("o")}}"};
            localStorage.setItem("{{localStorageName}}", JSON.stringify(store));
        });
    });
</script>
""");
    }
}