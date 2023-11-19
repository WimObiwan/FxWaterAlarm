
namespace Site;

public class AuthenticationMailOptions
{
    public const string Location = "AuthenticationMail";
        
    public required string Sender { get; init; }
    public required string SmtpServer { get; init; }
    public required string SmtpUsername { get; init; }
    public required string SmtpPassword { get; init; }
    public required TimeSpan? TokenLifespan { get; init; }
}
