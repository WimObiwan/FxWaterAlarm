using Core.Queries;
using Microsoft.Extensions.Options;
using Site.Pages;
using Site.Security;
using SiteTests.Helpers;
using Xunit;

namespace SiteTests.Security;

public class KnownLoginEmailServiceTest
{
    private static KnownLoginEmailService CreateService(
        ConfigurableFakeMediator mediator,
        string[]? adminEmails = null)
    {
        var options = new AccountLoginMessageOptions
        {
            TokenLifespanRaw = TimeSpan.FromHours(24),
            CodeLifespanHoursRaw = 2,
            SaltRaw = "test-salt",
            AdminEmails = adminEmails
        };
        return new KnownLoginEmailService(mediator, Options.Create(options));
    }

    [Fact]
    public async Task IsKnownLoginEmail_QueriesDatabase()
    {
        var mediator = new ConfigurableFakeMediator();
        mediator.SetResponse<IsKnownLoginEmailQuery, bool>(true);
        var service = CreateService(mediator);

        Assert.True(await service.IsKnownLoginEmail("user@test.com"));
        Assert.Contains(mediator.SentRequests, r => r is IsKnownLoginEmailQuery);
    }

    [Fact]
    public async Task IsKnownLoginEmail_UnknownInDatabase_ReturnsFalse()
    {
        var mediator = new ConfigurableFakeMediator();
        mediator.SetResponse<IsKnownLoginEmailQuery, bool>(false);
        var service = CreateService(mediator);

        Assert.False(await service.IsKnownLoginEmail("stranger@example.com"));
    }

    [Fact]
    public async Task IsKnownLoginEmail_AdminEmail_ReturnsTrueWithoutQuery()
    {
        var mediator = new ConfigurableFakeMediator();
        mediator.SetResponse<IsKnownLoginEmailQuery, bool>(false);
        var service = CreateService(mediator, new[] { "admin@test.com" });

        Assert.True(await service.IsKnownLoginEmail("ADMIN@test.com"));
        Assert.DoesNotContain(mediator.SentRequests, r => r is IsKnownLoginEmailQuery);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task IsKnownLoginEmail_BlankEmail_ReturnsFalse(string? email)
    {
        var mediator = new ConfigurableFakeMediator();
        mediator.SetResponse<IsKnownLoginEmailQuery, bool>(true);
        var service = CreateService(mediator);

        Assert.False(await service.IsKnownLoginEmail(email));
    }

    [Fact]
    public void IsAdminEmail_NoAdminEmailsConfigured_ReturnsFalse()
    {
        var service = CreateService(new ConfigurableFakeMediator());

        Assert.False(service.IsAdminEmail("admin@test.com"));
    }

    [Fact]
    public void IsAdminEmail_MatchesCaseInsensitively()
    {
        var service = CreateService(new ConfigurableFakeMediator(), new[] { "Admin@Test.com" });

        Assert.True(service.IsAdminEmail("admin@test.com"));
        Assert.False(service.IsAdminEmail("other@test.com"));
    }
}
