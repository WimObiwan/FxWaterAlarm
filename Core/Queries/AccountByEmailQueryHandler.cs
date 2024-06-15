using Core.Entities;
using Core.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Core.Queries;

public record AccountByEmailQuery : IRequest<Account?>
{
    public required string Email { get; init; }
}

public class AccountByEmailQueryHandler : IRequestHandler<AccountByEmailQuery, Account?>
{
    private readonly WaterAlarmDbContext _dbContext;

    public AccountByEmailQueryHandler(WaterAlarmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Account?> Handle(AccountByEmailQuery request, CancellationToken cancellationToken)
    {
        return await _dbContext.Accounts
            .Where(a => a.Email == request.Email)
            .Include(a => a.AccountSensors)
            .ThenInclude(as2 => as2.Sensor)
            .SingleOrDefaultAsync(cancellationToken);
    }
}