using Core.Entities;
using Core.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Core.Queries;

public record AccountByUidQuery : IRequest<Account?>
{
    public required Guid Uid { get; init; }
}

public class AccountByUidQueryHandler : IRequestHandler<AccountByUidQuery, Account?>
{
    private readonly WaterAlarmDbContext _dbContext;

    public AccountByUidQueryHandler(WaterAlarmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Account?> Handle(AccountByUidQuery request, CancellationToken cancellationToken)
    {
        return await _dbContext.Accounts
            .Where(a => a.Uid == request.Uid)
            .Include(a => a.AccountSensors.Where(@as => !@as.Disabled).OrderBy(@as => @as.Order))
            .ThenInclude(@as => @as.Sensor)
            .SingleOrDefaultAsync(cancellationToken);
    }
}
