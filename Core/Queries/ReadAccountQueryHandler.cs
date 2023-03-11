using Core.Entities;
using Core.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Core.Queries;

public class ReadAccountQuery : IRequest<Account?>
{
    public required Guid Uid { get; init; }
}

public class ReadAccountQueryHandler : IRequestHandler<ReadAccountQuery, Account?>
{
    private readonly WaterAlarmDbContext _dbContext;

    public ReadAccountQueryHandler(WaterAlarmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Account?> Handle(ReadAccountQuery request, CancellationToken cancellationToken)
    {
        return await _dbContext.Accounts
            .Where(a => a.Uid == request.Uid)
            .Include(a => a.AccountSensors)
            .ThenInclude(as2 => as2.Sensor)
            .SingleOrDefaultAsync(cancellationToken);
    }
}