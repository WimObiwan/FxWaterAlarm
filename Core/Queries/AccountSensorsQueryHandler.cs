using Core.Entities;
using Core.Exceptions;
using Core.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Core.Queries;

public record AccountSensorsQuery : IRequest<IEnumerable<AccountSensor>>
{
    public required Guid Uid { get; init; }
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
        var accountSensors =
            await _dbContext.Accounts
                .Where(a => a.Uid == request.Uid)
                .Include(a => a.AccountSensors)
                .ThenInclude(as2 => as2.Sensor)
                .SingleOrDefaultAsync(cancellationToken)
            ?? throw new AccountNotFoundException("The account cannot be found.")
                { AccountUid = request.Uid };
        return accountSensors.AccountSensors;
    }
}