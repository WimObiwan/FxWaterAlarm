using Core.Entities;
using Core.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Core.Queries;

public record AccountsQuery : IRequest<List<Account>>
{
    public bool IncludeAccountSensors { get; init; } = false;
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
        IQueryable<Account> accounts =
            _dbContext.Accounts;
        if (request.IncludeAccountSensors)
        {
            accounts = accounts
                .Include(a => a.AccountSensors
                    .Where(@as => !@as.Disabled))
                .ThenInclude(@as => @as.Sensor);
        }

        return await accounts
            .ToListAsync(cancellationToken);
    }
}