using Core.Entities;
using Core.Exceptions;
using Core.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Core.Queries;

public record AccountSensorByIdQuery : IRequest<AccountSensor?>
{
    public required Guid AccountUid { get; init; }
    public required Guid SensorUid { get; init; }
}

public class AccountSensorByIdQueryHandler : IRequestHandler<AccountSensorByIdQuery, AccountSensor?>
{
    private readonly WaterAlarmDbContext _dbContext;

    public AccountSensorByIdQueryHandler(WaterAlarmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AccountSensor?> Handle(AccountSensorByIdQuery request, CancellationToken cancellationToken)
    {
        var accountSensor =
            await _dbContext.Accounts
                .Where(a => a.Uid == request.AccountUid)
                .SelectMany(a => a.AccountSensors)
                .Where(@as => @as.Sensor.Uid == request.SensorUid && !@as.Disabled)
                .Include(@as => @as.Account)
                .Include(@as => @as.Sensor)
                .SingleOrDefaultAsync(cancellationToken);

        return accountSensor;
    }
}