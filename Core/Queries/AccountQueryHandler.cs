using Core.Entities;
using Core.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Core.Queries;

public record AccountQuery : IRequest<Account?>
{
    public required Guid Uid { get; init; }
}

public class AccountQueryHandler : IRequestHandler<AccountQuery, Account?>
{
    private readonly WaterAlarmDbContext _dbContext;

    public AccountQueryHandler(WaterAlarmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Account?> Handle(AccountQuery request, CancellationToken cancellationToken)
    {
        return await _dbContext.Accounts
            .Where(a => a.Uid == request.Uid)
            .Include(a => a.AccountSensors)
            .ThenInclude(as2 => as2.Sensor)
            .SingleOrDefaultAsync(cancellationToken);
    }
}