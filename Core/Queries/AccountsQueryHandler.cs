using Core.Entities;
using Core.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Core.Queries;

public record AccountsQuery : IRequest<List<Account>>
{
}

public class AccountsQueryHandler : IRequestHandler<AccountsQuery, List<Account>>
{
    private readonly WaterAlarmDbContext _dbContext;

    public AccountsQueryHandler(WaterAlarmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<Account>> Handle(AccountsQuery request, CancellationToken cancellationToken)
    {
        return await _dbContext.Accounts.ToListAsync(cancellationToken);
    }
}