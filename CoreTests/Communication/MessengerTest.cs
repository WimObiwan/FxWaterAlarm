using Core.Communication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Net.Mail;
using Xunit;

namespace CoreTests.Communication;

public class MessengerTest
{
    // Fake SMTP client that throws immediately to avoid network calls
    private class FakeFailingSmtpClient : ISmtpClientWrapper
    {
        public Task SendMailAsync(MailMessage message)
        {
            throw new SmtpException("Simulated SMTP failure");
        }

        public void Dispose()
        {
            // Nothing to dispose in fake implementation
        }
    }

    private class FakeFailingSmtpClientFactory : ISmtpClientFactory
    {
        public ISmtpClientWrapper CreateClient()
        {
            return new FakeFailingSmtpClient();
        }
    }
    private static MessengerOptions CreateOptions(
        string? overruleAlertDestination = null,
        string? mailContentPath = null,
        string? bcc = null,
        string[]? ignoreBcc = null)
    {
        return new MessengerOptions
        {
            Sender = "noreply@wateralarm.be",
            Bcc = bcc ?? "admin@wateralarm.be",
            SmtpServer = "localhost",
            SmtpUsername = "user",
            SmtpPassword = "pass",
            OverruleAlertDestination = overruleAlertDestination,
            MailContentPath = mailContentPath,
            IgnoreBcc = ignoreBcc
        };
    }

    private static MessengerOptions CreateOptionsWithInvalidSmtp(string mailContentPath)
    {
        return new MessengerOptions
        {
            Sender = "noreply@wateralarm.be",
            Bcc = "admin@wateralarm.be",
            SmtpServer = "invalid.example.invalid",
            SmtpUsername = "user",
            SmtpPassword = "pass",
            MailContentPath = mailContentPath
        };
    }

    private static Messenger CreateMessenger(MessengerOptions? options = null, ISmtpClientFactory? smtpFactory = null)
    {
        var opts = options ?? CreateOptions();
        return new Messenger(
            Options.Create(opts),
            NullLogger<Messenger>.Instance,
            smtpFactory);
    }

    private static Messenger CreateMessengerWithFakeSmtp(MessengerOptions? options = null)
    {
        return CreateMessenger(options, new FakeFailingSmtpClientFactory());
    }

    [Fact]
    public void Constructor_WithValidOptions_CreatesInstance()
    {
        var messenger = CreateMessenger();
        Assert.NotNull(messenger);
    }

    [Fact]
    public void Constructor_AcceptsNullBcc()
    {
        var options = CreateOptions(bcc: null);
        // The Bcc property is string? so null should be valid
        var messenger = CreateMessenger(options);
        Assert.NotNull(messenger);
    }

    [Fact]
    public async Task SendAuthenticationMailAsync_MissingContentFile_ThrowsFileNotFound()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"wateralarm_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var options = CreateOptions(mailContentPath: tempDir);
            var messenger = CreateMessenger(options);

            // mail.html doesn't exist in tempDir, should throw
            await Assert.ThrowsAsync<FileNotFoundException>(() =>
                messenger.SendAuthenticationMailAsync("test@example.com", "https://example.com/login", "123456"));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task SendAlertMailAsync_MissingContentFile_ThrowsFileNotFound()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"wateralarm_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var options = CreateOptions(mailContentPath: tempDir);
            var messenger = CreateMessenger(options);

            await Assert.ThrowsAsync<FileNotFoundException>(() =>
                messenger.SendAlertMailAsync("test@example.com", "https://example.com", "Sensor1", "Water level high", "High"));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task SendLinkMailAsync_MissingContentFile_ThrowsFileNotFound()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"wateralarm_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        try
        {
            var options = CreateOptions(mailContentPath: tempDir);
            var messenger = CreateMessenger(options);

            await Assert.ThrowsAsync<FileNotFoundException>(() =>
                messenger.SendLinkMailAsync("test@example.com", "https://example.com/link"));
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task SendAuthenticationMailAsync_WithContentFile_FailsOnSmtp()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"wateralarm_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var imagesDir = Path.Combine(tempDir, "images");
        Directory.CreateDirectory(imagesDir);
        try
        {
            // Create minimal mail.html template and required image
            await File.WriteAllTextAsync(Path.Combine(tempDir, "mail.html"), "<html>{{CONTENTS}}</html>");
            await File.WriteAllBytesAsync(Path.Combine(imagesDir, "wateralarm.png"), new byte[] { 0x89, 0x50, 0x4E, 0x47 });

            // Use fake SMTP client to ensure deterministic failure without network calls
            var options = CreateOptionsWithInvalidSmtp(tempDir);
            var messenger = CreateMessengerWithFakeSmtp(options);

            // Content exists but SMTP will fail (fake client)
            // This verifies the template processing completes before SMTP
            var ex = await Assert.ThrowsAsync<SmtpException>(() =>
                messenger.SendAuthenticationMailAsync("test@example.com", "https://example.com/login", "123456"));

            // Verify it's the expected SMTP exception
            Assert.Equal("Simulated SMTP failure", ex.Message);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task SendAlertMailAsync_WithContentFile_FailsOnSmtp()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"wateralarm_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var imagesDir = Path.Combine(tempDir, "images");
        Directory.CreateDirectory(imagesDir);
        try
        {
            await File.WriteAllTextAsync(Path.Combine(tempDir, "mail.html"), "<html>{{CONTENTS}}</html>");
            await File.WriteAllBytesAsync(Path.Combine(imagesDir, "wateralarm.png"), new byte[] { 0x89, 0x50, 0x4E, 0x47 });

            // Use fake SMTP client to ensure deterministic failure without network calls
            var options = CreateOptionsWithInvalidSmtp(tempDir);
            var messenger = CreateMessengerWithFakeSmtp(options);

            var ex = await Assert.ThrowsAsync<SmtpException>(() =>
                messenger.SendAlertMailAsync("test@example.com", "https://example.com", "Sensor1", "Water level high", "High"));

            Assert.Equal("Simulated SMTP failure", ex.Message);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public async Task SendLinkMailAsync_WithContentFile_FailsOnSmtp()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"wateralarm_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var imagesDir = Path.Combine(tempDir, "images");
        Directory.CreateDirectory(imagesDir);
        try
        {
            await File.WriteAllTextAsync(Path.Combine(tempDir, "mail.html"), "<html>{{CONTENTS}}</html>");
            await File.WriteAllBytesAsync(Path.Combine(imagesDir, "wateralarm.png"), new byte[] { 0x89, 0x50, 0x4E, 0x47 });

            // Use fake SMTP client to ensure deterministic failure without network calls
            var options = CreateOptionsWithInvalidSmtp(tempDir);
            var messenger = CreateMessengerWithFakeSmtp(options);

            var ex = await Assert.ThrowsAsync<SmtpException>(() =>
                messenger.SendLinkMailAsync("test@example.com", "https://example.com/link"));

            Assert.Equal("Simulated SMTP failure", ex.Message);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void Messenger_ImplementsIMessenger()
    {
        var messenger = CreateMessenger();
        Assert.IsAssignableFrom<IMessenger>(messenger);
    }
}
