using Core.Entities;
using Core.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Core.Queries;

/// <summary>
/// Returns true when the email address may be used to request a login code:
/// it is either a mail login of an account user, or the email address of an account.
/// </summary>
public record IsKnownLoginEmailQuery : IRequest<bool>
{
    public required string Email { get; init; }
}

public class IsKnownLoginEmailQueryHandler : IRequestHandler<IsKnownLoginEmailQuery, bool>
{
    private readonly WaterAlarmDbContext _dbContext;

    public IsKnownLoginEmailQueryHandler(WaterAlarmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> Handle(IsKnownLoginEmailQuery request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLower();

        if (string.IsNullOrEmpty(email))
            return false;

        var isAccountUser = await _dbContext.AccountUsers
            .AnyAsync(u => u.LoginType == AccountUserLoginType.Mail
                           && u.Email != null
                           && u.Email.ToLower() == email, cancellationToken);

        if (isAccountUser)
            return true;

        return await _dbContext.Accounts
            .AnyAsync(a => a.Email.ToLower() == email, cancellationToken);
    }
}
