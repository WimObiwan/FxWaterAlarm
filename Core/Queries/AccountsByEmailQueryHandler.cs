using Core.Entities;
using Core.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Core.Queries;

public record AccountsByEmailQuery : IRequest<IReadOnlyList<Account>>
{
    public required string Email { get; init; }
}

public class AccountsByEmailQueryHandler : IRequestHandler<AccountsByEmailQuery, IReadOnlyList<Account>>
{
    private readonly WaterAlarmDbContext _dbContext;

    public AccountsByEmailQueryHandler(WaterAlarmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Account>> Handle(AccountsByEmailQuery request, CancellationToken cancellationToken)
    {
        var accountIds = await _dbContext.AccountUsers
            .Where(u => u.LoginType == AccountUserLoginType.Mail
                        && u.Email != null
                        && u.Email.ToLower() == request.Email.ToLower())
            .Select(u => u.AccountId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return await _dbContext.Accounts
            .Where(a => accountIds.Contains(a.Id))
            .OrderBy(a => a.Id)
            .ToListAsync(cancellationToken);
    }
}
