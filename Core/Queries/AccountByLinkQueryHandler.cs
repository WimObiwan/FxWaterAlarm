using Core.Entities;
using Core.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Core.Queries;

public record AccountByLinkQuery : IRequest<Account?>
{
    public required string Link { get; init; }
}

public class AccountByLinkQueryHandler : IRequestHandler<AccountByLinkQuery, Account?>
{
    private readonly WaterAlarmDbContext _dbContext;

    public AccountByLinkQueryHandler(WaterAlarmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Account?> Handle(AccountByLinkQuery request, CancellationToken cancellationToken)
    {
        return await _dbContext.Accounts
            .Where(a => a.Link == request.Link)
            .Include(a => a.AccountSensors.Where(@as => !@as.Disabled).OrderBy(@as => @as.Order))
            .ThenInclude(@as => @as.Sensor)
            .SingleOrDefaultAsync(cancellationToken);
    }
}