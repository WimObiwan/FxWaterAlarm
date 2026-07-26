using Core.Queries;
using MediatR;
using Microsoft.Extensions.Options;
using Site.Pages;

namespace Site.Security;

/// <summary>
/// Decides which email addresses may be used to log in.  Login codes are only sent to
/// addresses known by this service; unknown addresses are rejected so the login form
/// cannot be used to mail arbitrary recipients.
/// </summary>
public interface IKnownLoginEmailService
{
    Task<bool> IsKnownLoginEmail(string? emailAddress, CancellationToken cancellationToken = default);
    bool IsAdminEmail(string? emailAddress);
}

public class KnownLoginEmailService : IKnownLoginEmailService
{
    private readonly IMediator _mediator;
    private readonly AccountLoginMessageOptions _options;

    public KnownLoginEmailService(IMediator mediator, IOptions<AccountLoginMessageOptions> options)
    {
        _mediator = mediator;
        _options = options.Value;
    }

    public async Task<bool> IsKnownLoginEmail(string? emailAddress, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(emailAddress))
            return false;

        if (IsAdminEmail(emailAddress))
            return true;

        return await _mediator.Send(new IsKnownLoginEmailQuery { Email = emailAddress }, cancellationToken);
    }

    public bool IsAdminEmail(string? emailAddress)
    {
        if (string.IsNullOrWhiteSpace(emailAddress))
            return false;

        return _options.AdminEmails?
            .Any(adminEmail => string.Equals(adminEmail.Trim(), emailAddress.Trim(), StringComparison.OrdinalIgnoreCase))
            ?? false;
    }
}
