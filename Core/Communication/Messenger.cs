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

public interface ISmtpClientWrapper
{
    Task SendMailAsync(MailMessage message);
}

internal class SmtpClientWrapper : ISmtpClientWrapper
{
    private readonly SmtpClient _smtpClient;

    public SmtpClientWrapper(string server, int port, string username, string password, bool enableSsl)
    {
        _smtpClient = new SmtpClient(server)
        {
            Port = port,
            Credentials = new NetworkCredential(username, password),
            EnableSsl = enableSsl
        };
    }

    public async Task SendMailAsync(MailMessage message)
    {
        await _smtpClient.SendMailAsync(message);
    }
}

public interface ISmtpClientFactory
{
    ISmtpClientWrapper CreateClient();
}

internal class SmtpClientFactory : ISmtpClientFactory
{
    private readonly MessengerOptions _options;

    public SmtpClientFactory(MessengerOptions options)
    {
        _options = options;
    }

    public ISmtpClientWrapper CreateClient()
    {
        return new SmtpClientWrapper(
            _options.SmtpServer,
            587,
            _options.SmtpUsername,
            _options.SmtpPassword,
            true);
    }
}

public interface IMessenger
{
    Task SendAuthenticationMailAsync(string emailAddress, string url, string code);
    Task SendAlertMailAsync(string emailAddress, string url, string? accountSensorName, string alertMessage, string shortAlertMessage);
    Task SendLinkMailAsync(string emailAddress, string url);
}

public class Messenger : IMessenger
{
    private readonly MessengerOptions _messengerOptions;
    private readonly ILogger<Messenger> _logger;
    private readonly ISmtpClientFactory _smtpClientFactory;

    public Messenger(
        IOptions<MessengerOptions> messengerOptions,
        ILogger<Messenger> logger,
        ISmtpClientFactory? smtpClientFactory = null)
    {
        _messengerOptions = messengerOptions.Value;
        _logger = logger;
        _smtpClientFactory = smtpClientFactory ?? new SmtpClientFactory(_messengerOptions);
    }

    private string GetContentPath(string path)
    {
        string contentPath = _messengerOptions.MailContentPath
            ?? Path.Join(Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location), "Content");
        return Path.Join(contentPath, path);
    }
    
    public async Task SendAuthenticationMailAsync(string emailAddress, string url, string code)
    {
        string subject = "WaterAlarm Log-in code - {{LOGINCODE}}";

        string contents = """
            <h1>Uw log-in code<br>voor wateralarm.be</h1>
            <div class="code">{{LOGINCODE}}</div>
            <div class="description">
              Gebruik deze log-in code om aan te melden op wateralarm.be,<br>
              of klik op de knop hieronder om meteen aan te melden.
            </div>
            <a href="{{LOGINURL}}" class="btn">Inloggen</a>
            """;

        subject = subject
            .Replace("{{LOGINCODE}}", code);

        contents = contents
            .Replace("{{LOGINURL}}", url)
            .Replace("{{LOGINCODE}}", code);

        await SendMailAsync(emailAddress, subject, contents);
    }

    public async Task SendAlertMailAsync(string emailAddress, string url, string? accountSensorName, string alertMessage, string shortAlertMessage)
    {
        string subject = "WaterAlarm Melding - {{ACCOUNTSENSORNAME}} - {{SHORTALERTMESSAGE}}";

        string content = """
            <h1>Melding voor uw sensor<br/><strong>{{ACCOUNTSENSORNAME}}</strong></h1>
            <div class="description">
              Er is een melding voor uw sensor:<br>
              <strong>{{ALERTMESSAGE}}</strong>
            </div>
            <div class="description">
              <span>Bekijk de details van uw sensor met de knop hieronder.</span>
            </div>
            <a href="{{URL}}" class="btn">Inloggen</a>
            <p><small>
              Als je deze meldingen niet meer wilt ontvangen, dan kun je deze zelf afzetten in de instellingen van je sensor.
              Of je kunt ons vragen om dit te desactiveren als antwoord op deze melding.
              Duidt deze mail niet aan als SPAM of ONGEWENST, om te vermijden dat andere ontvangers de mails niet meer ontvangen.
            </small></p>
            """;

        content = content
            .Replace("{{URL}}", url)
            .Replace("{{ACCOUNTSENSORNAME}}", accountSensorName)
            .Replace("{{ALERTMESSAGE}}", alertMessage);
        subject = subject
            .Replace("{{ACCOUNTSENSORNAME}}", accountSensorName)
            .Replace("{{SHORTALERTMESSAGE}}", shortAlertMessage);

        await SendMailAsync(emailAddress, subject, content);
    }

    public async Task SendLinkMailAsync(string emailAddress, string url)
    {
        string subject = "WaterAlarm Link";

        string contents = """
    <div class="content">
      <h1>Uw link<br>voor WaterAlarm</h1>
      <div class="description">
        Je vroeg om jouw unieke sensor of account link opnieuw te ontvangen:<br/>
        <a href="{{URL}}" title="link">{{URL}}</a>
      </div>
      <a href="{{URL}}" class="btn">Uw sensor</a>
    </div>
""";

        contents = contents
            .Replace("{{URL}}", url);

        await SendMailAsync(emailAddress, subject, contents);
    }

    public async Task SendMailAsync(string emailAddress, string subject, string contents)
    {
        string body = await File.ReadAllTextAsync(GetContentPath("mail.html"));
        body = body
            .Replace("{{CONTENTS}}", contents);

        LinkedResource[] linkedResources =
        {
            new LinkedResource(GetContentPath("images/wateralarm.png"))
                { ContentId = "images-wateralarm.png",  ContentType = new ContentType("image/png") },
        };

        await SendMailAsync(emailAddress, subject, body, linkedResources);
    }

    private async Task SendMailAsync(string emailAddress, string subject, string body, IList<LinkedResource> linkedResources)
    {
        _logger.LogInformation("Sending mail to {destination}, Subject: {subject}", emailAddress, subject);

        if (_messengerOptions.OverruleAlertDestination is {} overruleDestination && !string.IsNullOrEmpty(overruleDestination))
        {
            _logger.LogWarning("Mail link to real address {emailAddress} overruled by {overruleDestination}", emailAddress, overruleDestination);
            emailAddress = overruleDestination;
        }

        var smtpClient = _smtpClientFactory.CreateClient();

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