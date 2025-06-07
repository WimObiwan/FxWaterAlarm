using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Core.Communication;

public class MessengerOptions
{
    public const string Location = "Messenger";

    public string? OverruleAlertDestination { get; init; }
    public required string Sender { get; init; }
    public required string? Bcc { get; init; }
    public required string SmtpServer { get; init; }
    public required string SmtpUsername { get; init; }
    public required string SmtpPassword { get; init; }
    public string? MailContentPath { get; init; }
    public string[]? IgnoreBcc { get; init; }
}

public interface IMessenger
{
    Task SendAuthenticationMailAsync(string emailAddress, string url, string code);
    Task SendAlertMailAsync(string emailAddress, string url, string? accountSensorName, string alertMessage, string shortAlertMessage);
}

public class Messenger : IMessenger
{
    private readonly MessengerOptions _messengerOptions;
    private readonly ILogger<Messenger> _logger;

    public Messenger(
        IOptions<MessengerOptions> messengerOptions,
        ILogger<Messenger> logger)
    {
        _messengerOptions = messengerOptions.Value;
        _logger = logger;
    }

    private string GetContentPath(string path)
    {
        string contentPath = _messengerOptions.MailContentPath
            ?? Path.Join(Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location), "Content");
        return Path.Join(contentPath, path);
    }
    
    public async Task SendAuthenticationMailAsync(string emailAddress, string url, string code)
    {
        string body = await File.ReadAllTextAsync(GetContentPath("authentication-mail.html"));
        body = body
            .Replace("{{LOGINURL}}", url)
            .Replace("{{LOGINCODE}}", code);

        LinkedResource[] linkedResources =
        {
            new LinkedResource(GetContentPath("images/wateralarm.png"))
                { ContentId = "images-wateralarm.png",  ContentType = new ContentType("image/png") },
            new LinkedResource(GetContentPath("images/check-icon.png"))
                { ContentId = "images-check-icon.png",  ContentType = new ContentType("image/png") },
        };

        await SendMailAsync(emailAddress, "WaterAlarm.be e-mail verificatie", body, linkedResources);
    }

    public async Task SendAlertMailAsync(string emailAddress, string url, string? accountSensorName, string alertMessage, string shortAlertMessage)
    {
        string subject = "WaterAlarm.be melding - {{ACCOUNTSENSORNAME}} - {{SHORTALERTMESSAGE}}";
        string body = await File.ReadAllTextAsync(GetContentPath("alert-mail.html"));

        body = body
            .Replace("{{URL}}", url)
            .Replace("{{ACCOUNTSENSORNAME}}", accountSensorName)
            .Replace("{{ALERTMESSAGE}}", alertMessage);
        subject = subject
            .Replace("{{ACCOUNTSENSORNAME}}", accountSensorName)
            .Replace("{{SHORTALERTMESSAGE}}", shortAlertMessage);

        LinkedResource[] linkedResources =
        {
            new LinkedResource(GetContentPath("images/wateralarm.png"))
                { ContentId = "images-wateralarm.png",  ContentType = new ContentType("image/png") },
            new LinkedResource(GetContentPath("images/warning.png"))
                { ContentId = "images-warning-icon.png",  ContentType = new ContentType("image/png") },
        };

        string emailAddressToUse;
        if (_messengerOptions.OverruleAlertDestination is {} overruleDestination && !string.IsNullOrEmpty(overruleDestination))
        {
            _logger.LogWarning("Mail alert to real address {emailAddress} overruled by {overruleDestination}", emailAddress, overruleDestination);
            emailAddressToUse = overruleDestination;
        }
        else
        {
            emailAddressToUse = emailAddress;
        }

        await SendMailAsync(emailAddressToUse, subject, body, linkedResources);
    }

    private async Task SendMailAsync(string emailAddress, string subject, string body, IList<LinkedResource> linkedResources)
    {
        _logger.LogInformation("Sending mail to {destination}, Subject: {subject}", emailAddress, subject);

        var smtpClient = new SmtpClient(_messengerOptions.SmtpServer)
        {
            Port = 587,
            Credentials = new NetworkCredential(_messengerOptions.SmtpUsername,
                _messengerOptions.SmtpPassword),
            EnableSsl = true,
        };

        var message = new MailMessage
        {
            From = new MailAddress(_messengerOptions.Sender),
            Subject = subject,
            IsBodyHtml = true
        };

        message.To.Add(new MailAddress(emailAddress));

        _logger.LogInformation("Using Bcc {bcc}", _messengerOptions.Bcc);
        if (_messengerOptions.Bcc is { } bcc
            && !string.Equals(bcc, emailAddress, StringComparison.OrdinalIgnoreCase)
            && (_messengerOptions.IgnoreBcc == null || !_messengerOptions.IgnoreBcc.Contains(emailAddress, StringComparer.OrdinalIgnoreCase)))
            message.Bcc.Add(bcc);

        AlternateView view = AlternateView.CreateAlternateViewFromString(body, null, MediaTypeNames.Text.Html);

        foreach (var linkedResource in linkedResources)
            view.LinkedResources.Add(linkedResource);

        message.AlternateViews.Add(view);
        
        await smtpClient.SendMailAsync(message);
    } 
}