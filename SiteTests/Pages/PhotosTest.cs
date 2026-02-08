using Microsoft.AspNetCore.Mvc;
using Site.Pages;
using SiteTests.Helpers;

namespace SiteTests.Pages;

public class PhotosTest
{
    [Fact]
    public void OnGet_RedirectsToGooglePhotos()
    {
        var model = new Photos();
        TestEntityFactory.SetupPageContext(model);

        var result = model.OnGet();

        var redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("https://photos.app.goo.gl/2KgbPVx412SULxkK9", redirect.Url);
    }
}
