using Site.Pages;

namespace SiteTests.Pages;

public class AccountLoginMessageOptionsTest
{
    [Fact]
    public void Location_IsCorrectValue()
    {
        Assert.Equal("AccountLoginMessage", AccountLoginMessageOptions.Location);
    }

    [Fact]
    public void TokenLifespan_ReturnsConfiguredValue()
    {
        var options = new AccountLoginMessageOptions
        {
            TokenLifespanRaw = TimeSpan.FromHours(24),
            CodeLifespanHoursRaw = 2,
            SaltRaw = "test-salt"
        };

        Assert.Equal(TimeSpan.FromHours(24), options.TokenLifespan);
    }

    [Fact]
    public void TokenLifespan_Throws_WhenNull()
    {
        var options = new AccountLoginMessageOptions
        {
            TokenLifespanRaw = null,
            CodeLifespanHoursRaw = 2,
            SaltRaw = "test-salt"
        };

        Assert.Throws<Exception>(() => options.TokenLifespan);
    }

    [Fact]
    public void CodeLifespanHours_ReturnsConfiguredValue()
    {
        var options = new AccountLoginMessageOptions
        {
            TokenLifespanRaw = TimeSpan.FromHours(24),
            CodeLifespanHoursRaw = 4,
            SaltRaw = "test-salt"
        };

        Assert.Equal(4, options.CodeLifespanHours);
    }

    [Fact]
    public void CodeLifespanHours_Throws_WhenNull()
    {
        var options = new AccountLoginMessageOptions
        {
            TokenLifespanRaw = TimeSpan.FromHours(24),
            CodeLifespanHoursRaw = null,
            SaltRaw = "test-salt"
        };

        Assert.Throws<Exception>(() => options.CodeLifespanHours);
    }

    [Fact]
    public void Salt_ReturnsConfiguredValue()
    {
        var options = new AccountLoginMessageOptions
        {
            TokenLifespanRaw = TimeSpan.FromHours(24),
            CodeLifespanHoursRaw = 2,
            SaltRaw = "my-secret-salt"
        };

        Assert.Equal("my-secret-salt", options.Salt);
    }

    [Fact]
    public void Salt_Throws_WhenEmpty()
    {
        var options = new AccountLoginMessageOptions
        {
            TokenLifespanRaw = TimeSpan.FromHours(24),
            CodeLifespanHoursRaw = 2,
            SaltRaw = ""
        };

        Assert.Throws<Exception>(() => options.Salt);
    }

    [Fact]
    public void AdminIPs_ReturnsNull_WhenNotConfigured()
    {
        var options = new AccountLoginMessageOptions
        {
            TokenLifespanRaw = TimeSpan.FromHours(24),
            CodeLifespanHoursRaw = 2,
            SaltRaw = "salt",
            AdminIPsRaw = null
        };

        Assert.Null(options.AdminIPs);
    }

    [Fact]
    public void AdminIPs_ParsesIPRanges()
    {
        var options = new AccountLoginMessageOptions
        {
            TokenLifespanRaw = TimeSpan.FromHours(24),
            CodeLifespanHoursRaw = 2,
            SaltRaw = "salt",
            AdminIPsRaw = new[] { "192.168.1.0/24", "10.0.0.1" }
        };

        var adminIps = options.AdminIPs!.ToList();
        Assert.Equal(2, adminIps.Count);
    }

    [Fact]
    public void AdminEmails_DefaultsToNull()
    {
        var options = new AccountLoginMessageOptions
        {
            TokenLifespanRaw = TimeSpan.FromHours(24),
            CodeLifespanHoursRaw = 2,
            SaltRaw = "salt"
        };

        Assert.Null(options.AdminEmails);
    }

    [Fact]
    public void AdminEmails_ReturnsConfiguredValues()
    {
        var options = new AccountLoginMessageOptions
        {
            TokenLifespanRaw = TimeSpan.FromHours(24),
            CodeLifespanHoursRaw = 2,
            SaltRaw = "salt",
            AdminEmails = new[] { "admin1@test.com", "admin2@test.com" }
        };

        Assert.Equal(2, options.AdminEmails!.Length);
    }
}

public class AccountLoginMessageStaticTest
{
    [Fact]
    public void GetUrl_Basic_BuildsCorrectUrl()
    {
        var url = AccountLoginMessage.GetUrl(1, "my-link", "/return");

        Assert.Contains("m=1", url);
        Assert.Contains("a=my-link", url);
        Assert.Contains("r=%2freturn", url, StringComparison.OrdinalIgnoreCase);
        Assert.StartsWith("/Account/LoginMessage?", url);
    }

    [Fact]
    public void GetUrl_OmitsNullParams()
    {
        var url = AccountLoginMessage.GetUrl(2, null, null);

        Assert.Contains("m=2", url);
        Assert.DoesNotContain("a=", url);
        Assert.DoesNotContain("r=", url);
    }

    [Fact]
    public void GetUrl_WithAllParams()
    {
        var url = AccountLoginMessage.GetUrl(3, "acct", "email@test.com", "cookie123", "/return");

        Assert.Contains("m=3", url);
        Assert.Contains("a=acct", url);
        Assert.Contains("e=email", url);
        Assert.Contains("c=cookie123", url);
        Assert.Contains("r=", url);
    }

    [Fact]
    public void GetUrl_2ParamOverload_OmitsEmailAndCookie()
    {
        var url = AccountLoginMessage.GetUrl(1, "link", "/ret");

        Assert.Contains("m=1", url);
        Assert.Contains("a=link", url);
        Assert.DoesNotContain("e=", url);
        Assert.DoesNotContain("c=", url);
    }
}
