using Core.Entities;
using Core.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Core.Queries;

public record AccountUserByEmailQuery : IRequest<AccountUser?>
{
    public required string Email { get; init; }
}

public class AccountUserByEmailQueryHandler : IRequestHandler<AccountUserByEmailQuery, AccountUser?>
{
    private readonly WaterAlarmDbContext _dbContext;

    public AccountUserByEmailQueryHandler(WaterAlarmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AccountUser?> Handle(AccountUserByEmailQuery request, CancellationToken cancellationToken)
    {
        return await _dbContext.AccountUsers
            .Where(u => u.LoginType == AccountUserLoginType.Mail
                        && u.Email != null
                        && u.Email.ToLower() == request.Email.ToLower())
            .OrderBy(u => u.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
