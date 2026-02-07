using Core.Communication;
using Xunit;

namespace CoreTests;

public class MessengerOptionsTest
{
    [Fact]
    public void Location_ReturnsExpectedValue()
    {
        Assert.Equal("Messenger", MessengerOptions.Location);
    }

    [Fact]
    public void Properties_SetAndGet()
    {
        var options = new MessengerOptions
        {
            Sender = "noreply@wateralarm.be",
            Bcc = "admin@wateralarm.be",
            SmtpServer = "smtp.example.com",
            SmtpUsername = "user",
            SmtpPassword = "pass",
            OverruleAlertDestination = "test@example.com",
            MailContentPath = "/content",
            IgnoreBcc = ["skip@example.com", "skip2@example.com"]
        };

        Assert.Equal("noreply@wateralarm.be", options.Sender);
        Assert.Equal("admin@wateralarm.be", options.Bcc);
        Assert.Equal("smtp.example.com", options.SmtpServer);
        Assert.Equal("user", options.SmtpUsername);
        Assert.Equal("pass", options.SmtpPassword);
        Assert.Equal("test@example.com", options.OverruleAlertDestination);
        Assert.Equal("/content", options.MailContentPath);
        Assert.Equal(2, options.IgnoreBcc!.Length);
        Assert.Contains("skip@example.com", options.IgnoreBcc);
    }

    [Fact]
    public void NullableProperties_DefaultToNull()
    {
        var options = new MessengerOptions
        {
            Sender = "noreply@wateralarm.be",
            Bcc = null,
            SmtpServer = "smtp.example.com",
            SmtpUsername = "user",
            SmtpPassword = "pass"
        };

        Assert.Null(options.OverruleAlertDestination);
        Assert.Null(options.Bcc);
        Assert.Null(options.MailContentPath);
        Assert.Null(options.IgnoreBcc);
    }
}
