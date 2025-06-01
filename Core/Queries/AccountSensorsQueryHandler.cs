using Core.Entities;
using Core.Exceptions;
using Core.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Core.Queries;

public record AccountSensorsQuery : IRequest<IEnumerable<AccountSensor>>
{
    public Guid? AccountUid { get; init; } = null;
    public bool IncludeDisabled { get; init; } = false;
}

public class AccountSensorsQueryHandler : IRequestHandler<AccountSensorsQuery, IEnumerable<AccountSensor>>
{
    private readonly WaterAlarmDbContext _dbContext;

    public AccountSensorsQueryHandler(WaterAlarmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<AccountSensor>> Handle(AccountSensorsQuery request,
        CancellationToken cancellationToken)
    {
        if (request.AccountUid != null)
        {
            var accounts =
                await _dbContext.Accounts
                    .Where(a => a.Uid == request.AccountUid)
                    .Include(a => a.AccountSensors
                        .Where(@as => request.IncludeDisabled || !@as.Disabled)
                        .OrderBy(@as => @as.Order))
                    .ThenInclude(@as => @as.Sensor)
                    .SingleOrDefaultAsync(cancellationToken)
                ?? throw new AccountNotFoundException("The account cannot be found.")
                { AccountUid = request.AccountUid };

            return accounts.AccountSensors;
        }
        else
        {
            return
                await _dbContext.Accounts
                    .Include(@as => @as.AccountSensors
                       .Where(@as => request.IncludeDisabled || !@as.Disabled)
                        .OrderBy(@as => @as.Order))
                    .ThenInclude(@as => @as.Sensor)
                    .SelectMany(a => a.AccountSensors)
                    .ToListAsync(cancellationToken);

        }
    }
}