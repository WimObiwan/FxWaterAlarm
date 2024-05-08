using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using Microsoft.Extensions.Options;

namespace Core.Communication;

public class MessengerOptions
{
    public const string Location = "Messenger";
        
    public required string Sender { get; init; }
    public required string SmtpServer { get; init; }
    public required string SmtpUsername { get; init; }
    public required string SmtpPassword { get; init; }
}

public interface IMessenger
{
    Task SendAuthenticationMailAsync(string emailAddress, string url, string code);
    Task SendAlertMailAsync(string emailAddress, string? url);
}

public class Messenger : IMessenger
{
    private readonly MessengerOptions _messengerOptions;

    public Messenger(IOptions<MessengerOptions> messengerOptions)
    {
        _messengerOptions = messengerOptions.Value;
    }
    
    public async Task SendAuthenticationMailAsync(string emailAddress, string url, string code)
    {
        string body = await File.ReadAllTextAsync("Content/authentication-mail.html");
        body = body.Replace("{{LOGINURL}}", url);
        body = body.Replace("{{LOGINCODE}}", code);

        LinkedResource[] linkedResources =
        {
            new LinkedResource("Content/images/wateralarm.png")
                { ContentId = "images-wateralarm.png",  ContentType = new ContentType("image/png") },
            new LinkedResource("Content/images/check-icon.png")
                { ContentId = "images-check-icon.png",  ContentType = new ContentType("image/png") },
            new LinkedResource("Content/images/Beefree-logo.png")
                { ContentId = "images-Beefree-logo.png",  ContentType = new ContentType("image/png") }
        };

        await SendMailAsync(emailAddress, "WaterAlarm.be e-mail verificatie", body, linkedResources);
    }

    public async Task SendAlertMailAsync(string emailAddress, string? url)
    {
        string body = await File.ReadAllTextAsync("Content/alert-mail.html");
        body = body.Replace("{{URL}}", url ?? "");

        LinkedResource[] linkedResources =
        {
            new LinkedResource("Content/images/wateralarm.png")
                { ContentId = "images-wateralarm.png",  ContentType = new ContentType("image/png") },
            new LinkedResource("Content/images/check-icon.png")
                { ContentId = "images-check-icon.png",  ContentType = new ContentType("image/png") },
            new LinkedResource("Content/images/Beefree-logo.png")
                { ContentId = "images-Beefree-logo.png",  ContentType = new ContentType("image/png") }
        };

        await SendMailAsync(emailAddress, "WaterAlarm.be e-mail verificatie", body, linkedResources);
    }

    private async Task SendMailAsync(string emailAddress, string subject, string body, IList<LinkedResource> linkedResources)
    {
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

        AlternateView view = AlternateView.CreateAlternateViewFromString(body, null, MediaTypeNames.Text.Html);

        foreach (var linkedResource in linkedResources)
            view.LinkedResources.Add(linkedResource);

        message.AlternateViews.Add(view);
        
        await smtpClient.SendMailAsync(message);
    } 
}