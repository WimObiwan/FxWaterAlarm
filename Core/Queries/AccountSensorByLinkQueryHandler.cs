using Core.Entities;
using Core.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Core.Queries;

public record AccountSensorByLinkQuery : IRequest<AccountSensor?>
{
    public required string SensorLink { get; init; }
    public string? AccountLink { get; init; }
}

public class AccountSensorByLinkQueryHandler : IRequestHandler<AccountSensorByLinkQuery, AccountSensor?>
{
    private readonly WaterAlarmDbContext _dbContext;

    public AccountSensorByLinkQueryHandler(WaterAlarmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AccountSensor?> Handle(AccountSensorByLinkQuery request, CancellationToken cancellationToken)
    {
        var query = _dbContext.Sensors
            .Where(s => s.Link == request.SensorLink || s.DevEui == request.SensorLink)
            .SelectMany(s => s.AccountSensors);

        if (request.AccountLink != null)
            query = query.Where(as2 => as2.Account.Link == request.AccountLink);

        var accountSensor = await query
            .Include(as2 => as2.Account)
            .ThenInclude(a => a.AccountSensors)
            .Include(as2 => as2.Sensor)
            .SingleOrDefaultAsync(cancellationToken);
        
        if (accountSensor != null)
            accountSensor.EnsureEnabled();

        return accountSensor;
    }
}