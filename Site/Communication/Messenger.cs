using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using QRCoder;

namespace Site.Communication;

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

        var smtpClient = new SmtpClient(_messengerOptions.SmtpServer)
        {
            Port = 587,
            Credentials = new NetworkCredential(_messengerOptions.SmtpUsername,
                _messengerOptions.SmtpPassword),
            EnableSsl = true,
        };

        string body = await File.ReadAllTextAsync("Content/authentication-mail.html");
        body = body.Replace("{{LOGINURL}}", url);
        body = body.Replace("{{LOGINCODE}}", code);

        var message = new MailMessage
        {
            From = new MailAddress(_messengerOptions.Sender),
            Subject = "WaterAlarm.be e-mail verificatie",
            IsBodyHtml = true
        };

        message.To.Add(new MailAddress(emailAddress));

        AlternateView view = AlternateView.CreateAlternateViewFromString(body, null, MediaTypeNames.Text.Html);

        LinkedResource resource = new LinkedResource("Content/images/wateralarm.png")
            { ContentId = "images-wateralarm.png",  ContentType = new ContentType("image/png") };
        view.LinkedResources.Add(resource);

        resource = new LinkedResource("Content/images/check-icon.png")
            { ContentId = "images-check-icon.png",  ContentType = new ContentType("image/png") };
        view.LinkedResources.Add(resource);

        resource = new LinkedResource("Content/images/Beefree-logo.png")
            { ContentId = "images-Beefree-logo.png",  ContentType = new ContentType("image/png") };
        view.LinkedResources.Add(resource);
        
        message.AlternateViews.Add(view);
        
        await smtpClient.SendMailAsync(message);
    }
}